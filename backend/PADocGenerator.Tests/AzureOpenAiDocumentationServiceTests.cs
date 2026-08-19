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
            Nodes =
            [
                new ParsedFlowNode
                {
                    Id = "trigger",
                    Name = "manual",
                    Type = "Request",
                    NodeType = "Trigger",
                    Inputs = new Dictionary<string, string>(),
                    UsedVariables = new List<string>()
                },
                new ParsedFlowNode
                {
                    Id = "send-email",
                    Name = "Envoyer un email",
                    Type = "SendEmail",
                    NodeType = "Action",
                    ConnectorReference = "Office 365",
                    Inputs = new Dictionary<string, string>(),
                    UsedVariables = new List<string>()
                },
                new ParsedFlowNode
                {
                    Id = "notify-manager",
                    Name = "Notifier le responsable",
                    Type = "PostMessage",
                    NodeType = "Action",
                    ConnectorReference = "Teams",
                    Inputs = new Dictionary<string, string>(),
                    UsedVariables = new List<string>()
                },
                new ParsedFlowNode
                {
                    Id = "validation",
                    Name = "Validation",
                    Type = "If",
                    NodeType = "Condition",
                    Inputs = new Dictionary<string, string>
                    {
                        ["expression"] = "status = approved"
                    },
                    UsedVariables = new List<string>()
                }
            ],
            Edges =
            [
                new ParsedFlowEdge
                {
                    SourceId = "trigger",
                    TargetId = "send-email",
                    Label = null
                },
                new ParsedFlowEdge
                {
                    SourceId = "send-email",
                    TargetId = "validation",
                    Label = null
                },
                new ParsedFlowEdge
                {
                    SourceId = "validation",
                    TargetId = "notify-manager",
                    Label = "Oui"
                }
            ]
        };

        var result = await service.GenerateAsync(parsedFlow, CancellationToken.None);

        result.FunctionalSummary.Should().Contain("Flux de validation");

        // Le fallback actuel documente tous les Nodes, y compris le Trigger.
        result.Steps.Should().HaveCount(4);

        result.Steps.Should().Contain(s =>
            s.StepName == "Envoyer un email" &&
            s.Connector == "Office 365");

        result.Steps.Should().Contain(s =>
            s.StepName == "Notifier le responsable" &&
            s.Connector == "Teams");

        result.Steps.Should().Contain(s =>
            s.StepName == "Validation" &&
            s.StepType == "If");

        result.Dependencies.Should().Contain(d =>
            d.From == "Validation" &&
            d.To == "Notifier le responsable" &&
            d.RelationshipType == "Oui");

        result.Diagram.Nodes.Should().HaveCount(4);
        result.Diagram.Edges.Should().HaveCount(3);
    }
}