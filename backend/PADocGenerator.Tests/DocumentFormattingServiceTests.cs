using FluentAssertions;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

public class DocumentFormattingServiceTests
{
    private readonly DocumentFormattingService _sut = new();

    private static DocumentationDiagramDto EmptyDiagram() =>
        new(
            new List<DocumentationDiagramNodeDto>(),
            new List<DocumentationDiagramEdgeDto>()
        );

    [Fact]
    public void Format_TrimsFunctionalSummary()
    {
        var raw = new DocumentationContentDto(
            "  résumé avec espaces  ",
            new List<DocumentationStepDto>(),
            new List<DocumentationDependencyDto>(),
            EmptyDiagram());

        var formatted = _sut.Format(raw);

        formatted.FunctionalSummary.Should().Be("résumé avec espaces");
    }

    [Fact]
    public void Format_PreservesSteps()
    {
        var raw = new DocumentationContentDto(
            "résumé",
            new List<DocumentationStepDto>
            {
                new(
                    "step-1",
                    "Étape secondaire",
                    "Compose",
                    null,
                    "description B",
                    "Préparer les données.",
                    new List<DocumentationVariableDto>(),
                    new Dictionary<string, string>()
                ),
                new(
                    "step-2",
                    "Étape critique",
                    "Condition",
                    null,
                    "description A",
                    "Vérifier une condition.",
                    new List<DocumentationVariableDto>(),
                    new Dictionary<string, string>()
                )
            },
            new List<DocumentationDependencyDto>(),
            EmptyDiagram());

        var formatted = _sut.Format(raw);

        formatted.Steps.Should().HaveCount(2);
        formatted.Steps.Select(s => s.StepName).Should()
            .BeEquivalentTo("Étape secondaire", "Étape critique");
    }

    [Fact]
    public void Format_DeduplicatesDependenciesByFromTo()
    {
        var raw = new DocumentationContentDto(
            "résumé",
            new List<DocumentationStepDto>(),
            new List<DocumentationDependencyDto>
            {
                new("A", "B", "première explication", "Exécution"),
                new("A", "B", "explication dupliquée", "Exécution"),
                new("B", "C", "autre lien", "Oui")
            },
            EmptyDiagram());

        var formatted = _sut.Format(raw);

        formatted.Dependencies.Should().HaveCount(2);
    }
}