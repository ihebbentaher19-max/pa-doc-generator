using PADocGenerator.Api.Models.Dtos;

namespace PADocGenerator.Api.Services.Interfaces;

/// <summary>
/// Module de recherche et consultation (section 6) : recherche par mot-clé,
/// nom ou statut.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Un Administrateur (<paramref name="isAdmin"/> = true) voit toutes les
    /// documentations de la plateforme ; un Utilisateur ne voit que celles
    /// qu'il a créées (section 6 du cahier des charges).
    /// </summary>
    Task<(List<DocumentationSummaryDto> Items, int TotalCount)> SearchAsync(
        SearchDocumentationQueryDto query, Guid currentUserId, bool isAdmin, CancellationToken ct = default);
}
