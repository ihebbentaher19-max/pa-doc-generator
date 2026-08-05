using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PADocGenerator.Api.Models.FlowSchema;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

public class AzureOpenAiDocumentationServiceTests
{
    [Fact]
    public async Task GenerateAsync_WhenAzureCallFails_ReturnsFallbackDocumentation()
    {
        var service = new AzureOpenAiDocumentationService(
            Options.Create(new AzureOpenAiOptions
            {
                Endpoint = "http://127.0.0.1:1",
                ApiKey = "dummy-key",
                DeploymentName = "dummy-deployment"
            }),
            new PromptBuilderService(),
            NullLogger<AzureOpenAiDocumentationService>.Instance);

        var parsedFlow = new ParsedFlow
        {
            FlowName = "Flux de validation",
            Trigger = "manual",
            Actions =
            [
                new ParsedAction { Name = "Envoyer un email", Type = "Office 365" },
                new ParsedAction { Name = "Notifier le responsable", Type = "Teams" }
            ],
            Conditions =
            [
                new ParsedCondition { Name = "Validation", Expression = "status = approved", ActionsIfTrue = ["Notifier le responsable"], ActionsIfFalse = ["Rejeter la demande"] }
            ]
        };

        var result = await service.GenerateAsync(parsedFlow, CancellationToken.None);

        result.FunctionalSummary.Should().Contain("Flux de validation");
        result.Steps.Should().HaveCount(2);
        result.ImportantSteps.Should().Contain("Envoyer un email");
    }
}
