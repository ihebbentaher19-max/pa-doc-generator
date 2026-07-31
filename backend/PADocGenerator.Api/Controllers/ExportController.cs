using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Controllers;

/// <summary>Module d'export (section 6) : génération de fichiers téléchargeables
/// PDF ou Word pour partage hors plateforme / archivage.</summary>
[ApiController]
[Route("api/documentation/{id:guid}/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IDocumentManagementService _managementService;
    private readonly IExportService _exportService;

    public ExportController(IDocumentManagementService managementService, IExportService exportService)
    {
        _managementService = managementService;
        _exportService = exportService;
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken ct) => await Export(id, ExportFormat.Pdf, ct);

    [HttpGet("word")]
    public async Task<IActionResult> ExportWord(Guid id, CancellationToken ct) => await Export(id, ExportFormat.Word, ct);

    private async Task<IActionResult> Export(Guid id, ExportFormat format, CancellationToken ct)
    {
        var documentation = await _managementService.GetByIdAsync(id, ct);
        if (documentation is null) return NotFound();

        if (!User.CanModify(documentation.CreatedByUserId))
            return Forbid();

        var (content, fileName, contentType) = await _exportService.ExportAsync(documentation, format, ct);
        return File(content, contentType, fileName);
    }
}
