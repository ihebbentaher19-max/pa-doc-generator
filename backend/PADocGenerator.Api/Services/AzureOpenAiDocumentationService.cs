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

            return ParseModelResponse(ExtractJsonPayload(rawJson));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec de l'appel Azure OpenAI pour le flux {FlowName}", parsedFlow.FlowName);
            return BuildFallbackDocumentation(parsedFlow);
        }
    }

    private static DocumentationContentDto BuildFallbackDocumentation(ParsedFlow flow)
    {
        var summary = $"Documentation de secours pour le flux {flow.FlowName}. " +
            "Les étapes principales sont listées ci-dessous en l'absence de réponse du service IA.";

        var steps = flow.Actions.Select(action => new DocumentationStepDto(
                action.Name,
                string.IsNullOrWhiteSpace(action.Type)
                    ? "Action du flux sans type précisé."
                    : $"Action de type {action.Type}.",
                false))
            .ToList();

        var dependencies = new List<DocumentationDependencyDto>();
        foreach (var condition in flow.Conditions)
        {
            foreach (var actionName in condition.ActionsIfTrue)
            {
                dependencies.Add(new DocumentationDependencyDto(
                    condition.Name,
                    actionName,
                    $"Lorsque la condition '{condition.Name}' est vraie, '{actionName}' est exécutée."));
            }

            foreach (var actionName in condition.ActionsIfFalse)
            {
                dependencies.Add(new DocumentationDependencyDto(
                    condition.Name,
                    actionName,
                    $"Lorsque la condition '{condition.Name}' est fausse, '{actionName}' est exécutée."));
            }
        }

        var importantSteps = flow.Actions.Select(a => a.Name).ToList();

        return new DocumentationContentDto(summary, steps, dependencies, importantSteps);
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

    private static DocumentationContentDto ParseModelResponse(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var summary = root.GetProperty("functionalSummary").GetString() ?? string.Empty;

        var steps = new List<DocumentationStepDto>();
        if (root.TryGetProperty("steps", out var stepsEl))
        {
            foreach (var step in stepsEl.EnumerateArray())
            {
                steps.Add(new DocumentationStepDto(
                    step.GetProperty("stepName").GetString() ?? string.Empty,
                    step.GetProperty("description").GetString() ?? string.Empty,
                    step.TryGetProperty("isImportant", out var imp) && imp.GetBoolean()
                ));
            }
        }

        var dependencies = new List<DocumentationDependencyDto>();
        if (root.TryGetProperty("dependencies", out var depsEl))
        {
            foreach (var dep in depsEl.EnumerateArray())
            {
                dependencies.Add(new DocumentationDependencyDto(
                    dep.GetProperty("from").GetString() ?? string.Empty,
                    dep.GetProperty("to").GetString() ?? string.Empty,
                    dep.GetProperty("explanationText").GetString() ?? string.Empty
                ));
            }
        }

        var importantSteps = new List<string>();
        if (root.TryGetProperty("importantSteps", out var impStepsEl))
        {
            importantSteps.AddRange(impStepsEl.EnumerateArray().Select(s => s.GetString() ?? string.Empty));
        }

        return new DocumentationContentDto(summary, steps, dependencies, importantSteps);
    }
}
