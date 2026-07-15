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
    public void Parse_RealisticFlow_ExtractsNonConditionNonVariableActions()
    {
        var parsed = _sut.Parse(SampleFlows.ApprovalFlowJson);

        parsed.Actions.Select(a => a.Name).Should()
            .BeEquivalentTo("Send_an_email", "Approve_request", "Reject_request");
    }

    [Fact]
    public void Parse_RealisticFlow_ExtractsOneConditionWithBothBranches()
    {
        var parsed = _sut.Parse(SampleFlows.ApprovalFlowJson);

        parsed.Conditions.Should().HaveCount(1);
        parsed.Conditions[0].ActionsIfTrue.Should().Contain("Approve_request");
        parsed.Conditions[0].ActionsIfFalse.Should().Contain("Reject_request");
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
    public void Parse_FirstAction_HasNoRunsAfterDependency()
    {
        var parsed = _sut.Parse(SampleFlows.ApprovalFlowJson);

        var sendEmail = parsed.Actions.First(a => a.Name == "Send_an_email");
        sendEmail.RunsAfter.Should().BeNull();
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

        parsed.Actions.Should().ContainSingle();
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

        parsed.Actions.Should().ContainSingle();
        parsed.FlowName.Should().Be("Flux sans nom");
    }
}
