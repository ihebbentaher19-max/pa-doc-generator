using FluentAssertions;
using PADocGenerator.Api.Models.Entities;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetStatsAsync_CountsFlowsAndDocumentationsByStatus()
    {
        var db = TestDbContextFactory.Create();

        var user = new ApplicationUser { FullName = "Jane Doe", Email = "jane@contoso.com" };
        var flow1 = new FlowImport { Name = "Flow 1", RawJson = "{}", IsValid = true, ImportedByUserId = user.Id };
        var flow2 = new FlowImport { Name = "Flow 2", RawJson = "{}", IsValid = true, ImportedByUserId = user.Id };
        db.Users.Add(user);
        db.FlowImports.AddRange(flow1, flow2);

        db.Documentations.AddRange(
            new Documentation { FlowImportId = flow1.Id, Title = "Doc 1", Status = DocumentationStatus.Brouillon, CreatedByUserId = user.Id },
            new Documentation { FlowImportId = flow1.Id, Title = "Doc 2", Status = DocumentationStatus.Brouillon, CreatedByUserId = user.Id },
            new Documentation { FlowImportId = flow2.Id, Title = "Doc 3", Status = DocumentationStatus.Valide, CreatedByUserId = user.Id },
            new Documentation { FlowImportId = flow2.Id, Title = "Doc 4", Status = DocumentationStatus.Archive, CreatedByUserId = user.Id }
        );
        db.SaveChanges();

        var sut = new DashboardService(db);
        var stats = await sut.GetStatsAsync(user.Id, isAdmin: true);

        stats.TotalFlowsImported.Should().Be(2);
        stats.TotalDocumentations.Should().Be(4);
        stats.DraftCount.Should().Be(2);
        stats.ValidatedCount.Should().Be(1);
        stats.ArchivedCount.Should().Be(1);
        stats.RecentDocumentations.Should().HaveCountLessOrEqualTo(10);
    }

    [Fact]
    public async Task GetStatsAsync_EmptyDatabase_ReturnsZeroes()
    {
        var db = TestDbContextFactory.Create();
        var sut = new DashboardService(db);

        var stats = await sut.GetStatsAsync(Guid.NewGuid(), isAdmin: true);

        stats.TotalDocumentations.Should().Be(0);
        stats.TotalFlowsImported.Should().Be(0);
        stats.RecentDocumentations.Should().BeEmpty();
    }
}
