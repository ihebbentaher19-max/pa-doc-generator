using PADocGenerator.Api.Models.Dtos;

namespace PADocGenerator.Api.Services.Interfaces;

/// <summary>
/// Module de tableau de bord (section 6) : nombre de documentations générées,
/// derniers documents créés, vue globale de l'activité.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Un Administrateur (<paramref name="isAdmin"/> = true) voit les
    /// statistiques globales de la plateforme ; un Utilisateur ne voit que
    /// ses propres flux importés et documentations générées (section 6 du
    /// cahier des charges : rôle Administrateur vs Utilisateur).
    /// </summary>
    Task<DashboardStatsDto> GetStatsAsync(Guid currentUserId, bool isAdmin, CancellationToken ct = default);
}
