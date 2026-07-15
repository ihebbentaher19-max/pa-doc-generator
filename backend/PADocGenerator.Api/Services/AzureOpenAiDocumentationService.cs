using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Models.FlowSchema;
using PADocGenerator.Api.Services.Interfaces;

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
/// Le prompt est construit par <see cref="PromptBuilderService"/> à partir du
/// <see cref="ParsedFlow"/> produit par le module de lecture/préparation ; le
/// modèle répond en JSON strict qui est désérialisé directement en
/// <see cref="DocumentationContentDto"/>.
/// </summary>
public class AzureOpenAiDocumentationService : IAiDocumentationService
{
    private readonly AzureOpenAIClient _client;
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

        _client = new AzureOpenAIClient(
            new Uri(_options.Endpoint),
            new System.ClientModel.ApiKeyCredential(_options.ApiKey));
    }

    public async Task<DocumentationContentDto> GenerateAsync(ParsedFlow parsedFlow, CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(_options.DeploymentName);

        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildUserPrompt(parsedFlow);

        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            new OpenAI.Chat.SystemChatMessage(systemPrompt),
            new OpenAI.Chat.UserChatMessage(userPrompt)
        };

        var chatOptions = new OpenAI.Chat.ChatCompletionOptions
        {
            Temperature = 0.2f,
            ResponseFormat = OpenAI.Chat.ChatResponseFormat.CreateJsonObjectFormat()
        };

        try
        {
            var response = await chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);
            var rawJson = response.Value.Content[0].Text;

            return ParseModelResponse(rawJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec de l'appel Azure OpenAI pour le flux {FlowName}", parsedFlow.FlowName);
            throw new InvalidOperationException(
                "La génération de documentation via Azure OpenAI a échoué. Vérifiez la configuration " +
                "AzureOpenAI:Endpoint / ApiKey / DeploymentName.", ex);
        }
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
