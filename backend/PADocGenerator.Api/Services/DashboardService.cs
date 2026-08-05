using Microsoft.EntityFrameworkCore;
using PADocGenerator.Api.Data;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Models.Entities;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Implémentation du module de tableau de bord (section 6) : nombre de
/// documentations générées, répartition par statut, dernières activités.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(Guid currentUserId, bool isAdmin, CancellationToken ct = default)
    {
        var documentations = _db.Documentations.AsQueryable();
        var flowImports = _db.FlowImports.AsQueryable();

        if (!isAdmin)
        {
            documentations = documentations.Where(d => d.CreatedByUserId == currentUserId);
            flowImports = flowImports.Where(f => f.ImportedByUserId == currentUserId);
        }

        var totalDocumentations = await documentations.CountAsync(ct);
        var totalFlowsImported = await flowImports.CountAsync(ct);

        var draftCount = await documentations.CountAsync(d => d.Status == DocumentationStatus.Brouillon, ct);
        var validatedCount = await documentations.CountAsync(d => d.Status == DocumentationStatus.Valide, ct);
        var archivedCount = await documentations.CountAsync(d => d.Status == DocumentationStatus.Archive, ct);

        var recent = await documentations
            .Include(d => d.FlowImport)
            .Include(d => d.CreatedByUser)
            .OrderByDescending(d => d.UpdatedAtUtc)
            .Take(10)
            .Select(d => new DocumentationSummaryDto(
                d.Id,
                d.Title,
                d.FlowImport != null ? d.FlowImport.Name : "Flux inconnu",
                d.Status.ToString(),
                d.CurrentVersionNumber,
                d.CreatedByUser != null ? d.CreatedByUser.FullName : "Inconnu",
                d.UpdatedAtUtc))
            .ToListAsync(ct);

        return new DashboardStatsDto(
            totalDocumentations, totalFlowsImported, draftCount, validatedCount, archivedCount, recent);
    }
}
