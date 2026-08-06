using FluentAssertions;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

public class FlowValidationServiceTests
{
    private readonly FlowValidationService _sut = new();

    [Fact]
    public void Validate_EmptyContent_ReturnsInvalid()
    {
        var result = _sut.Validate("");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhitespaceContent_ReturnsInvalid()
    {
        var result = _sut.Validate("   ");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MalformedJson_ReturnsInvalid()
    {
        var result = _sut.Validate("{ this is not json ");
        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Validate_ValidJsonWithoutActionsOrTriggers_ReturnsInvalid()
    {
        const string json = """{ "name": "Flux vide", "properties": { "definition": { "foo": "bar" } } }""";
        var result = _sut.Validate(json);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_JsonStringLiteral_ReturnsInvalidBusinessMessage()
    {
        var result = _sut.Validate("\"ce n'est pas un flux\"");
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(UserMessages.InvalidFlowFormat);
    }

    [Fact]
    public void Validate_RealisticPowerAutomateExport_ReturnsValid()
    {
        var result = _sut.Validate(SampleFlows.ApprovalFlowJson);
        result.IsValid.Should().BeTrue(result.Error);
    }

    [Fact]
    public void Validate_FlatDefinitionWithoutPropertiesWrapper_ReturnsValid()
    {
        const string json = """
        {
          "definition": {
            "triggers": { "manual": { "type": "Request" } },
            "actions": { "Only_action": { "type": "Compose", "inputs": {}, "runAfter": {} } }
          }
        }
        """;
        var result = _sut.Validate(json);
        result.IsValid.Should().BeTrue(result.Error);
    }
}
