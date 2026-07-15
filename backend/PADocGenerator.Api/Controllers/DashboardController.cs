using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Controllers;

/// <summary>Module de tableau de bord (section 6) : affiche le nombre de
/// documentations générées, les dernières activités, une vue globale.</summary>
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
        return Ok(await _dashboardService.GetStatsAsync(ct));
    }
}
