using Microsoft.EntityFrameworkCore;
using PADocGenerator.Api.Data;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Models.Entities;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Implémentation du module de recherche et consultation (section 6) :
/// recherche par mot-clé (titre de la documentation ou nom du flux), et/ou
/// filtrage par statut.
/// </summary>
public class SearchService : ISearchService
{
    private readonly AppDbContext _db;

    public SearchService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(List<DocumentationSummaryDto> Items, int TotalCount)> SearchAsync(
        SearchDocumentationQueryDto query, Guid currentUserId, bool isAdmin, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var queryable = _db.Documentations
            .Include(d => d.FlowImport)
            .Include(d => d.CreatedByUser)
            .AsQueryable();

        if (!isAdmin)
        {
            queryable = queryable.Where(d => d.CreatedByUserId == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            queryable = queryable.Where(d =>
                EF.Functions.ILike(d.Title, $"%{keyword}%") ||
                (d.FlowImport != null && EF.Functions.ILike(d.FlowImport.Name, $"%{keyword}%")));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<DocumentationStatus>(query.Status, ignoreCase: true, out var status))
        {
            queryable = queryable.Where(d => d.Status == status);
        }

        var totalCount = await queryable.CountAsync(ct);

        var items = await queryable
            .OrderByDescending(d => d.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentationSummaryDto(
                d.Id,
                d.Title,
                d.FlowImport != null ? d.FlowImport.Name : "Flux inconnu",
                d.Status.ToString(),
                d.CurrentVersionNumber,
                d.CreatedByUser != null ? d.CreatedByUser.FullName : "Inconnu",
                d.UpdatedAtUtc))
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
