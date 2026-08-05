using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Data;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Models.Entities;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Controllers;

/// <summary>
/// Module d'importation (section 6) : chargement d'un fichier JSON représentant
/// un flux Power Automate, avec vérification de conformité au format attendu.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FlowsController : ControllerBase
{
    private readonly IFlowValidationService _validationService;
    private readonly IFlowParserService _parserService;
    private readonly AppDbContext _db;

    public FlowsController(IFlowValidationService validationService, IFlowParserService parserService, AppDbContext db)
    {
        _validationService = validationService;
        _parserService = parserService;
        _db = db;
    }

    /// <summary>Importe un flux Power Automate exporté au format JSON (collé ou envoyé en texte).</summary>
    [HttpPost("import")]
    public async Task<ActionResult<FlowImportResultDto>> Import(FlowImportRequestDto request, CancellationToken ct)
    {
        var validation = _validationService.Validate(request.JsonContent);

        var flowImport = new FlowImport
        {
            Name = string.IsNullOrWhiteSpace(request.FileName) ? "Flux sans nom" : request.FileName,
            RawJson = request.JsonContent,
            IsValid = validation.IsValid,
            ValidationError = validation.Error,
            ImportedByUserId = User.GetUserId()
        };

        if (validation.IsValid)
        {
            try
            {
                var parsed = _parserService.Parse(request.JsonContent);
                flowImport.ActionsCount = parsed.Actions.Count;
                flowImport.Name = string.IsNullOrWhiteSpace(request.FileName) ? parsed.FlowName : request.FileName;
            }
            catch
            {
                // La validation basique a réussi mais le parsing détaillé a échoué :
                // on conserve quand même l'import, il pourra être ré-analysé plus tard.
                flowImport.ActionsCount = 0;
            }
        }

        _db.FlowImports.Add(flowImport);
        await _db.SaveChangesAsync(ct);

        var result = new FlowImportResultDto(
            flowImport.Id, flowImport.Name, flowImport.IsValid, flowImport.ValidationError,
            flowImport.ActionsCount, flowImport.ImportedAtUtc);

        if (!validation.IsValid)
            return UnprocessableEntity(result);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FlowImportResultDto>> GetById(Guid id, CancellationToken ct)
    {
        var flow = await _db.FlowImports.FindAsync([id], ct);
        if (flow is null) return NotFound();

        if (!User.CanModify(flow.ImportedByUserId))
            return Forbid();

        return Ok(new FlowImportResultDto(flow.Id, flow.Name, flow.IsValid, flow.ValidationError, flow.ActionsCount, flow.ImportedAtUtc));
    }
}
