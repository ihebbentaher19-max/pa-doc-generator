using FluentAssertions;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

public class DocumentFormattingServiceTests
{
    private readonly DocumentFormattingService _sut = new();

    [Fact]
    public void Format_TrimsFunctionalSummary()
    {
        var raw = new DocumentationContentDto("  résumé avec espaces  ", new(), new(), new());

        var formatted = _sut.Format(raw);

        formatted.FunctionalSummary.Should().Be("résumé avec espaces");
    }

    [Fact]
    public void Format_OrdersImportantStepsFirst()
    {
        var raw = new DocumentationContentDto(
            "résumé",
            new List<DocumentationStepDto>
            {
                new("Étape secondaire", "description B", false),
                new("Étape critique", "description A", true),
            },
            new(), new());

        var formatted = _sut.Format(raw);

        formatted.Steps.First().IsImportant.Should().BeTrue();
        formatted.Steps.First().StepName.Should().Be("Étape critique");
    }

    [Fact]
    public void Format_DeduplicatesDependenciesByFromTo()
    {
        var raw = new DocumentationContentDto(
            "résumé",
            new(),
            new List<DocumentationDependencyDto>
            {
                new("A", "B", "première explication"),
                new("A", "B", "explication dupliquée"),
                new("B", "C", "autre lien"),
            },
            new());

        var formatted = _sut.Format(raw);

        formatted.Dependencies.Should().HaveCount(2);
    }

    [Fact]
    public void Format_DeduplicatesAndCleansImportantSteps()
    {
        var raw = new DocumentationContentDto(
            "résumé", new(), new(),
            new List<string> { "Étape critique", "Étape critique", "   ", "Autre étape importante" });

        var formatted = _sut.Format(raw);

        formatted.ImportantSteps.Should().HaveCount(2);
        formatted.ImportantSteps.Should().Contain("Étape critique");
        formatted.ImportantSteps.Should().Contain("Autre étape importante");
    }
}
