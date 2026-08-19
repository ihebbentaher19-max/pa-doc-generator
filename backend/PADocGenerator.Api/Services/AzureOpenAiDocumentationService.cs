using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Models.FlowSchema;
using PADocGenerator.Api.Services.Interfaces;

#pragma warning disable OPENAI001 // La Responses API est encore en préversion dans le SDK OpenAI/.NET.

namespace PADocGenerator.Api.Services;

public class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
}

/// <summary>
/// Implémentation du module de génération de documentation (section 6) via
/// Azure OpenAI (choix retenu section 5 : "Confidentialité des données métier
/// assurée par le tenant Azure de l'entreprise, intégration native à
/// l'écosystème Azure existant, aucune infrastructure à gérer.").
///
/// Utilise la Responses API — et non la Chat Completions API (ancienne
/// implémentation) — car le déploiement fourni par l'encadrant repose sur un
/// modèle de la famille GPT-5 / Codex, qui n'est exposé sur Azure OpenAI que
/// via cette API. Un appel Chat Completions sur ce type de modèle renvoie
/// HTTP 400 "The requested operation is unsupported".
///
/// La Responses API est servie sur la surface "v1" d'Azure OpenAI
/// (https://<ressource>.openai.azure.com/openai/v1), différente de l'ancienne
/// surface "/openai/deployments/{nom}/..." utilisée par la Chat Completions
/// API — d'où la construction d'URL spécifique dans le constructeur.
///
/// Le prompt est construit par <see cref="PromptBuilderService"/> à partir du
/// <see cref="ParsedFlow"/> produit par le module de lecture/préparation ; le
/// modèle répond en JSON strict (imposé par le prompt système, cf.
/// <see cref="PromptBuilderService.BuildSystemPrompt"/>) qui est désérialisé
/// directement en <see cref="DocumentationContentDto"/>.
/// </summary>
public class AzureOpenAiDocumentationService : IAiDocumentationService
{
    private readonly ResponsesClient _client;
    private readonly AzureOpenAiOptions _options;
    private readonly PromptBuilderService _promptBuilder;
    private readonly ILogger<AzureOpenAiDocumentationService> _logger;

    public AzureOpenAiDocumentationService(
        IOptions<AzureOpenAiOptions> options,
        PromptBuilderService promptBuilder,
        ILogger<AzureOpenAiDocumentationService> logger)
    {
        _options = options.Value;
        _promptBuilder = promptBuilder;
        _logger = logger;

        // AzureOpenAI:Endpoint reste stocké sous sa forme "classique"
        // (https://<ressource>.openai.azure.com/) ; on construit ici l'URL de
        // la surface "v1" requise par la Responses API, sans avoir à changer
        // la valeur du secret déjà configurée.
        var baseEndpoint = _options.Endpoint.TrimEnd('/');
        var responsesEndpoint = new Uri($"{baseEndpoint}/openai/v1");

        _client = new ResponsesClient(
            credential: new System.ClientModel.ApiKeyCredential(_options.ApiKey),
            options: new ResponsesClientOptions { Endpoint = responsesEndpoint });
    }

    public async Task<DocumentationContentDto> GenerateAsync(ParsedFlow parsedFlow, CancellationToken cancellationToken = default)
    {
        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildUserPrompt(parsedFlow);

        // AzureOpenAI:DeploymentName correspond au paramètre "Model" attendu
        // par la Responses API : c'est le nom du déploiement donné dans la
        // ressource Azure OpenAI (Model deployments), pas nécessairement le
        // nom du modèle sous-jacent (ex. gpt-5.1-codex-mini).
        var requestOptions = new CreateResponseOptions
        {
            Model = _options.DeploymentName,
            InputItems =
            {
                ResponseItem.CreateSystemMessageItem(systemPrompt),
                ResponseItem.CreateUserMessageItem(userPrompt)
            }
        };

        try
        {
            var result = await _client.CreateResponseAsync(requestOptions);
            var rawJson = result.Value.GetOutputText();

            return ParseModelResponse(ExtractJsonPayload(rawJson),parsedFlow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec de l'appel Azure OpenAI pour le flux {FlowName}", parsedFlow.FlowName);
            return BuildFallbackDocumentation(parsedFlow);
        }
    }
    private static string GetNodeName(
        ParsedFlow flow,
        string nodeId)
    {
        return flow.Nodes
            .FirstOrDefault(node => node.Id == nodeId)
            ?.Name ?? nodeId;
    }
    private static DocumentationContentDto BuildFallbackDocumentation(ParsedFlow flow)
    {
        var summary = $"Documentation de secours pour le flux {flow.FlowName}. " +
            "Les étapes principales sont listées ci-dessous en l'absence de réponse du service IA.";

        var steps = flow.Nodes
            .Select(node => new DocumentationStepDto(
                node.Id,
                node.Name,
                node.Type,
                node.ConnectorReference,
                string.IsNullOrWhiteSpace(node.Type)
                    ? "Étape technique extraite du flux."
                    : $"Étape technique de type {node.Type} extraite du flux.",
                node.NodeType switch
                {
                    "Trigger" => "Déclenche le démarrage du flux.",
                    "Condition" => "Contrôle le déroulement du flux selon une condition.",
                    "Loop" => "Répète le traitement selon la structure détectée dans le flux.",
                    "Variable" => "Manipule une variable détectée dans le flux.",
                    _ => "Participe au traitement technique du flux."
                },
                node.UsedVariables
                    .Select(variableName => new DocumentationVariableDto(
                        variableName,
                        flow.Variables
                            .FirstOrDefault(v => v.Name == variableName)
                            ?.InitialValue,
                        $"Variable utilisée par l'étape {node.Name}."))
                    .ToList(),
                node.Inputs))
            .ToList();

        var dependencies = flow.Edges
            .Select(edge => new DocumentationDependencyDto(
                GetNodeName(flow, edge.SourceId),
                GetNodeName(flow, edge.TargetId),
                string.IsNullOrWhiteSpace(edge.Label)
                    ? "Exécution entre les deux éléments du flux."
                    : $"Relation technique correspondant à la branche ou au lien « {edge.Label} ».",
                string.IsNullOrWhiteSpace(edge.Label)
                    ? "Exécution"
                    : edge.Label
            ))
            .ToList();
        var diagram = new DocumentationDiagramDto(
            flow.Nodes.Select(node => new DocumentationDiagramNodeDto(
                node.Id,
                node.Name,
                node.Type,
                node.NodeType)).ToList(),
            flow.Edges.Select(edge => new DocumentationDiagramEdgeDto(
                edge.SourceId,
                edge.TargetId,
                edge.Label)).ToList());

        return new DocumentationContentDto(
            summary,
            steps,
            dependencies,
            diagram);
    }

    /// <summary>
    /// Malgré la consigne du prompt système ("Réponds UNIQUEMENT avec un objet
    /// JSON valide"), certains modèles (notamment les modèles de la famille
    /// codex/raisonnement) enveloppent tout de même leur réponse dans un bloc
    /// de code Markdown (```json ... ``` ou ``` ... ```). Cette étape retire
    /// cet emballage avant la désérialisation stricte, sans avoir besoin de
    /// modifier le prompt ni de dépendre d'une option de formatage encore
    /// incertaine côté SDK Responses API.
    /// </summary>
    private static string ExtractJsonPayload(string rawText)
    {
        var text = rawText.Trim();

        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0)
            {
                text = text[(firstNewline + 1)..];
            }

            var closingFenceIndex = text.LastIndexOf("```", StringComparison.Ordinal);
            if (closingFenceIndex >= 0)
            {
                text = text[..closingFenceIndex];
            }

            text = text.Trim();
        }

        // Filet de sécurité supplémentaire : si du texte restait avant/après
        // l'objet JSON, on ne garde que ce qui se trouve entre la première
        // "{" et la dernière "}".
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end >= start)
        {
            text = text[start..(end + 1)];
        }

        return text;
    }

    private static DocumentationContentDto ParseModelResponse(string rawJson, ParsedFlow flow)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var summary = root.GetProperty("functionalSummary").GetString() ?? string.Empty;

        var steps = new List<DocumentationStepDto>();
        if (root.TryGetProperty("steps", out var stepsEl))
        {
            foreach (var step in stepsEl.EnumerateArray())
            {
                var stepId = step.TryGetProperty("stepId", out var idEl)
                    ? idEl.GetString() ?? string.Empty
                    : string.Empty;
                var sourceNode = flow.Nodes
                    .FirstOrDefault(node => node.Id == stepId);
                var variables = new List<DocumentationVariableDto>();

                if (sourceNode != null &&
                    step.TryGetProperty("variables", out var variablesEl) &&
                    variablesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var variable in variablesEl.EnumerateArray())
                    {
                        var variableName = variable.TryGetProperty("name", out var nameEl)
                            ? nameEl.GetString() ?? string.Empty
                            : string.Empty;

                        // La variable doit réellement être détectée dans le nœud source.
                        if (!sourceNode.UsedVariables.Contains(variableName))
                        {
                            continue;
                        }

                        var flowVariable = flow.Variables
                            .FirstOrDefault(v => v.Name == variableName);

                        variables.Add(new DocumentationVariableDto(
                            variableName,
                            flowVariable?.InitialValue,
                            variable.TryGetProperty("description", out var descriptionEl)
                                ? descriptionEl.GetString() ?? string.Empty
                                : string.Empty
                        ));
                    }
                }

                var inputs = sourceNode?.Inputs != null
                    ? new Dictionary<string, string>(sourceNode.Inputs)
                    : new Dictionary<string, string>();

                steps.Add(new DocumentationStepDto(
                    stepId,
                    sourceNode?.Name ??
                        (step.TryGetProperty("stepName", out var stepNameEl)
                            ? stepNameEl.GetString() ?? string.Empty
                            : string.Empty),
                    sourceNode?.Type ??
                        (step.TryGetProperty("stepType", out var typeEl)
                            ? typeEl.GetString() ?? string.Empty
                            : string.Empty),
                    sourceNode?.ConnectorReference,
                    step.TryGetProperty("description", out var stepDescriptionEl)
                        ? stepDescriptionEl.GetString() ?? string.Empty
                        : string.Empty,
                    step.TryGetProperty("purpose", out var purposeEl)
                        ? purposeEl.GetString() ?? string.Empty
                        : string.Empty,
                    variables,
                    inputs
                ));
            }
        }

        var dependencies = flow.Edges
            .Select(edge => new DocumentationDependencyDto(
                GetNodeName(flow, edge.SourceId),
                GetNodeName(flow, edge.TargetId),
                string.IsNullOrWhiteSpace(edge.Label)
                    ? "Exécution entre les deux éléments du flux."
                    : $"Relation technique correspondant à la branche ou au lien « {edge.Label} ». ",
                string.IsNullOrWhiteSpace(edge.Label)
                    ? "Exécution"
                    : edge.Label
            ))
            .ToList();
            
        var diagram = new DocumentationDiagramDto(
            flow.Nodes.Select(node => new DocumentationDiagramNodeDto(
                node.Id,
                node.Name,
                node.Type,
                node.NodeType
            )).ToList(),
            flow.Edges.Select(edge => new DocumentationDiagramEdgeDto(
                edge.SourceId,
                edge.TargetId,
                edge.Label
            )).ToList()
        );

    return new DocumentationContentDto(
        summary,
        steps,
        dependencies,
        diagram);
        }
}