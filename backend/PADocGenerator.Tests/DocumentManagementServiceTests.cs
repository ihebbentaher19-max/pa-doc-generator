using FluentAssertions;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Models.Entities;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

public class DocumentManagementServiceTests
{
    private static (DocumentManagementService Sut, Api.Data.AppDbContext Db, Guid UserId, Guid FlowId) CreateSutWithSeededFlow()
    {
        var db = TestDbContextFactory.Create();

        var user = new ApplicationUser { FullName = "Jane Doe", Email = "jane@contoso.com", Role = UserRole.Utilisateur };
        var flow = new FlowImport { Name = "Approval Flow", RawJson = "{}", IsValid = true, ImportedByUserId = user.Id };

        db.Users.Add(user);
        db.FlowImports.Add(flow);
        db.SaveChanges();

        return (new DocumentManagementService(db), db, user.Id, flow.Id);
    }

    private static DocumentationContentDto SampleContent(string summary = "Résumé initial") =>
        new(summary,
            new List<DocumentationStepDto> { new("Étape 1", "Description 1", true) },
            new List<DocumentationDependencyDto>(),
            new List<string> { "Étape 1" });

    [Fact]
    public async Task CreateFromGenerationAsync_CreatesDocumentationWithVersionOne()
    {
        var (sut, _, userId, flowId) = CreateSutWithSeededFlow();

        var result = await sut.CreateFromGenerationAsync(flowId, userId, SampleContent());

        result.CurrentVersionNumber.Should().Be(1);
        result.Status.Should().Be("Brouillon");
        result.Content.FunctionalSummary.Should().Be("Résumé initial");
    }

    [Fact]
    public async Task CreateFromGenerationAsync_UnknownFlow_ThrowsKeyNotFound()
    {
        var (sut, _, userId, _) = CreateSutWithSeededFlow();

        var act = () => sut.CreateFromGenerationAsync(Guid.NewGuid(), userId, SampleContent());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_CreatesNewVersionInsteadOfOverwriting()
    {
        var (sut, _, userId, flowId) = CreateSutWithSeededFlow();
        var created = await sut.CreateFromGenerationAsync(flowId, userId, SampleContent());

        var updated = await sut.UpdateAsync(
            created.Id, userId,
            new UpdateDocumentationDto("Titre modifié", SampleContent("Résumé modifié"), "Correction manuelle"));

        updated.CurrentVersionNumber.Should().Be(2);
        updated.Title.Should().Be("Titre modifié");

        var history = await sut.GetVersionHistoryAsync(created.Id);
        history.Should().HaveCount(2);
        history.Should().Contain(v => v.VersionNumber == 1 && !v.IsManuallyEdited);
        history.Should().Contain(v => v.VersionNumber == 2 && v.IsManuallyEdited);
    }

    [Fact]
    public async Task UpdateAsync_PersistsFullContentInNewVersion()
    {
        var (sut, _, userId, flowId) = CreateSutWithSeededFlow();
        var created = await sut.CreateFromGenerationAsync(flowId, userId, SampleContent());

        var updatedContent = new DocumentationContentDto(
            "Résumé modifié",
            new List<DocumentationStepDto> { new("Étape 2", "Description 2", true) },
            new List<DocumentationDependencyDto> { new("Étape 1", "Étape 2", "Dépendance de test") },
            new List<string> { "Étape 2" });

        await sut.UpdateAsync(
            created.Id,
            userId,
            new UpdateDocumentationDto("Titre modifié", updatedContent, "Correction manuelle"));

        var version = await sut.GetVersionAsync(created.Id, 2);
        version.Content.FunctionalSummary.Should().Be("Résumé modifié");
        version.Content.Steps.Should().ContainSingle(s => s.StepName == "Étape 2" && s.IsImportant);
        version.Content.Dependencies.Should().ContainSingle(d => d.From == "Étape 1" && d.To == "Étape 2");
        version.Content.ImportantSteps.Should().ContainSingle(s => s == "Étape 2");
    }

    [Fact]
    public async Task ChangeStatusAsync_ValidStatus_UpdatesStatus()
    {
        var (sut, _, userId, flowId) = CreateSutWithSeededFlow();
        var created = await sut.CreateFromGenerationAsync(flowId, userId, SampleContent());

        var updated = await sut.ChangeStatusAsync(created.Id, "Valide");

        updated.Status.Should().Be("Valide");
    }

    [Fact]
    public async Task ChangeStatusAsync_InvalidStatus_ThrowsArgumentException()
    {
        var (sut, _, userId, flowId) = CreateSutWithSeededFlow();
        var created = await sut.CreateFromGenerationAsync(flowId, userId, SampleContent());

        var act = () => sut.ChangeStatusAsync(created.Id, "StatutInexistant");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var (sut, _, _, _) = CreateSutWithSeededFlow();

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesDocumentation()
    {
        var (sut, _, userId, flowId) = CreateSutWithSeededFlow();
        var created = await sut.CreateFromGenerationAsync(flowId, userId, SampleContent());

        await sut.DeleteAsync(created.Id);

        var result = await sut.GetByIdAsync(created.Id);
        result.Should().BeNull();
    }
}
