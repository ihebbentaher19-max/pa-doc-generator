using FluentAssertions;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Models.Entities;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

/// <summary>
/// IMPORTANT : <see cref="SearchService"/> utilise <c>EF.Functions.ILike</c>
/// pour la recherche par mot-clé, une fonction traduite en SQL par le
/// fournisseur Npgsql. Le fournisseur EF Core InMemory (utilisé ici pour
/// éviter une vraie base PostgreSQL) NE SAIT PAS traduire cette fonction et
/// lève une <see cref="InvalidOperationException"/> si on l'exécute. Les tests
/// ci-dessous couvrent donc uniquement les chemins qui n'utilisent pas
/// <c>ILike</c> (filtre par statut, pagination, absence de mot-clé). Une
/// couverture complète du chemin "recherche par mot-clé" nécessite un test
/// d'intégration contre un vrai PostgreSQL (ex. via Testcontainers), à
/// ajouter séparément.
/// </summary>
public class SearchServiceTests
{
    private static (SearchService Sut, Api.Data.AppDbContext Db) CreateSutWithSeededData()
    {
        var db = TestDbContextFactory.Create();

        var user = new ApplicationUser { FullName = "Jane Doe", Email = "jane@contoso.com" };
        var flow = new FlowImport { Name = "Approval Flow", RawJson = "{}", IsValid = true, ImportedByUserId = user.Id };
        db.Users.Add(user);
        db.FlowImports.Add(flow);

        var docs = new[]
        {
            new Documentation { FlowImportId = flow.Id, Title = "Doc brouillon", Status = DocumentationStatus.Brouillon, CreatedByUserId = user.Id },
            new Documentation { FlowImportId = flow.Id, Title = "Doc validee", Status = DocumentationStatus.Valide, CreatedByUserId = user.Id },
            new Documentation { FlowImportId = flow.Id, Title = "Doc archivee", Status = DocumentationStatus.Archive, CreatedByUserId = user.Id },
        };
        db.Documentations.AddRange(docs);
        db.SaveChanges();

        return (new SearchService(db), db);
    }

    [Fact]
    public async Task SearchAsync_NoFilters_ReturnsAllDocumentations()
    {
        var (sut, _) = CreateSutWithSeededData();

        var (items, totalCount) = await sut.SearchAsync(new SearchDocumentationQueryDto(null, null));

        totalCount.Should().Be(3);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_FilterByStatus_ReturnsOnlyMatchingDocumentations()
    {
        var (sut, _) = CreateSutWithSeededData();

        var (items, totalCount) = await sut.SearchAsync(new SearchDocumentationQueryDto(null, "Valide"));

        totalCount.Should().Be(1);
        items.Should().ContainSingle(d => d.Title == "Doc validee");
    }

    [Fact]
    public async Task SearchAsync_Pagination_RespectsPageSize()
    {
        var (sut, _) = CreateSutWithSeededData();

        var (items, totalCount) = await sut.SearchAsync(new SearchDocumentationQueryDto(null, null, Page: 1, PageSize: 2));

        totalCount.Should().Be(3); // le total ignore la pagination
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_InvalidStatusString_IsIgnoredNotThrown()
    {
        var (sut, _) = CreateSutWithSeededData();

        var (items, totalCount) = await sut.SearchAsync(new SearchDocumentationQueryDto(null, "StatutInexistant"));

        // Enum.TryParse échoue silencieusement -> le filtre de statut n'est pas appliqué
        totalCount.Should().Be(3);
    }
}
