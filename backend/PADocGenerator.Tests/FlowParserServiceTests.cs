using FluentAssertions;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

public class FlowParserServiceTests
{
    private readonly FlowParserService _sut = new();

    [Fact]
    public void Parse_RealisticFlow_ExtractsFlowNameAndTrigger()
    {
        var parsed = _sut.Parse(SampleFlows.ApprovalFlowJson);

        parsed.FlowName.Should().Be("Approval Flow");
        parsed.Trigger.Should().Be("manual");
    }

    [Fact]
public void Parse_RealisticFlow_ExtractsActionNodes()
{
    var parsed = _sut.Parse(SampleFlows.ApprovalFlowJson);

    parsed.Nodes
        .Where(node => node.NodeType == "Action")
        .Select(node => node.Name)
        .Should()
        .BeEquivalentTo(
            "Send an email",
            "Initialize counter",
            "Approve request",
            "Reject request");
}

    [Fact]
public void Parse_RealisticFlow_ExtractsConditionAndBothBranches()
{
    var parsed = _sut.Parse(SampleFlows.ApprovalFlowJson);

    var condition = parsed.Nodes
        .Single(node => node.NodeType == "Condition");

    condition.Name.Should().Be("Condition");

    parsed.Edges.Should().Contain(edge =>
        edge.SourceId == condition.Id &&
        edge.TargetId == "Approve_request" &&
        edge.Label == "Oui");

    parsed.Edges.Should().Contain(edge =>
        edge.SourceId == condition.Id &&
        edge.TargetId == "Reject_request" &&
        edge.Label == "Non");
}

    [Fact]
    public void Parse_RealisticFlow_ExtractsVariableFromVariablesArraySchema()
    {
        var parsed = _sut.Parse(SampleFlows.ApprovalFlowJson);

        parsed.Variables.Should().ContainSingle();
        parsed.Variables[0].Name.Should().Be("counter");
        parsed.Variables[0].Type.Should().Be("integer");
    }

    [Fact]
    public void Parse_RealisticFlow_ExtractsDistinctConnectors()
    {
        var parsed = _sut.Parse(SampleFlows.ApprovalFlowJson);

        parsed.Connectors.Select(c => c.Name).Should()
            .BeEquivalentTo("shared_office365", "shared_sharepointonline");
    }

    [Fact]
    public void Parse_FirstAction_IsConnectedToTrigger()
    {
        var parsed = _sut.Parse(SampleFlows.ApprovalFlowJson);

        parsed.Edges.Should().Contain(edge =>
            edge.SourceId == "manual" &&
            edge.TargetId == "Send_an_email");
    }

    [Fact]
    public void Parse_HttpActionWithoutConnectorHost_DoesNotThrowAndAddsNoConnector()
    {
        const string json = """
        {
          "name": "Http Flow",
          "definition": {
            "triggers": { "manual": { "type": "Request" } },
            "actions": {
              "Call_external_api": {
                "type": "Http",
                "inputs": { "method": "GET", "uri": "https://example.com/api" },
                "runAfter": {}
              }
            }
          }
        }
        """;

        var parsed = _sut.Parse(json);

        parsed.Nodes
          .Where(node => node.NodeType == "Action")
          .Should()
          .ContainSingle();
        parsed.Connectors.Should().BeEmpty();
    }

    [Fact]
    public void Parse_FlowWithoutPropertiesWrapper_StillParsesDefinitionDirectly()
    {
        const string json = """
        {
          "definition": {
            "triggers": { "manual": { "type": "Request" } },
            "actions": { "Only_action": { "type": "Compose", "inputs": { "inputs": "hello" }, "runAfter": {} } }
          }
        }
        """;

        var parsed = _sut.Parse(json);

        parsed.Nodes
          .Where(node => node.NodeType == "Action")
          .Should()
          .ContainSingle();
        parsed.FlowName.Should().Be("Flux sans nom");
    }
}
