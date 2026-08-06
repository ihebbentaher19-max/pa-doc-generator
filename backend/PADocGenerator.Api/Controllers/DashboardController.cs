using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Controllers;

/// <summary>Module de tableau de bord (section 6) : affiche le nombre de
/// documentations générées, les dernières activités. Vue globale pour un
/// administrateur, vue limitée à ses propres données pour un utilisateur
/// (module de gestion des rôles, section 6).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardStatsDto>> Get(CancellationToken ct)
    {
        var stats = await _dashboardService.GetStatsAsync(
            User.GetUserId(), User.IsInRole("Administrateur"), ct);
        return Ok(stats);
    }
}
