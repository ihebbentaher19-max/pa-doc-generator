using PADocGenerator.Api.Models.Dtos;

namespace PADocGenerator.Api.Services.Interfaces;

/// <summary>
/// Module de recherche et consultation (section 6) : recherche par mot-clé,
/// nom ou statut.
/// </summary>
public interface ISearchService
{
    Task<(List<DocumentationSummaryDto> Items, int TotalCount)> SearchAsync(SearchDocumentationQueryDto query, CancellationToken ct = default);
}
