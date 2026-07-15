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

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var totalDocumentations = await _db.Documentations.CountAsync(ct);
        var totalFlowsImported = await _db.FlowImports.CountAsync(ct);

        var draftCount = await _db.Documentations.CountAsync(d => d.Status == DocumentationStatus.Brouillon, ct);
        var validatedCount = await _db.Documentations.CountAsync(d => d.Status == DocumentationStatus.Valide, ct);
        var archivedCount = await _db.Documentations.CountAsync(d => d.Status == DocumentationStatus.Archive, ct);

        var recent = await _db.Documentations
            .Include(d => d.FlowImport)
            .OrderByDescending(d => d.UpdatedAtUtc)
            .Take(10)
            .Select(d => new DocumentationSummaryDto(
                d.Id,
                d.Title,
                d.FlowImport != null ? d.FlowImport.Name : "Flux inconnu",
                d.Status.ToString(),
                d.CurrentVersionNumber,
                d.UpdatedAtUtc))
            .ToListAsync(ct);

        return new DashboardStatsDto(
            totalDocumentations, totalFlowsImported, draftCount, validatedCount, archivedCount, recent);
    }
}
