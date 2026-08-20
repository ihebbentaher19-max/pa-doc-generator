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
    private readonly IPowerPlatformFlowService _powerPlatformFlowService;
    private readonly AppDbContext _db;

    public FlowsController(
        IFlowValidationService validationService,
        IFlowParserService parserService,
        IPowerPlatformFlowService powerPlatformFlowService,
        AppDbContext db)
    {
        _validationService = validationService;
        _parserService = parserService;
        _powerPlatformFlowService = powerPlatformFlowService;
        _db = db;
    }

    /// <summary>
    /// Retourne les environnements Power Platform que le compte Microsoft connecté
    /// peut utiliser. Requiert le jeton délégué transmis temporairement par le SPA.
    /// </summary>
    [HttpGet("power-platform/environments")]
    public async Task<ActionResult<IReadOnlyList<PowerPlatformEnvironmentDto>>> GetPowerPlatformEnvironments(CancellationToken ct)
    {
        var environments = await _powerPlatformFlowService.GetEnvironmentsAsync(GetRequiredToken("X-PowerPlatform-Access-Token"), ct);
        return Ok(environments);
    }

    /// <summary>Retourne les flux cloud accessibles dans un environnement sélectionné.</summary>
    [HttpGet("power-platform/environments/{environmentId}/flows")]
    public async Task<ActionResult<IReadOnlyList<PowerPlatformFlowDto>>> GetPowerPlatformFlows(string environmentId, CancellationToken ct)
    {
        var flows = await _powerPlatformFlowService.GetFlowsAsync(
            GetRequiredToken("X-PowerPlatform-Access-Token"), environmentId, ct);
        return Ok(flows);
    }

    /// <summary>Importe un flux Power Automate exporté au format JSON (collé ou envoyé en texte).</summary>
    [HttpPost("import")]
    public async Task<ActionResult<FlowImportResultDto>> Import(FlowImportRequestDto request, CancellationToken ct)
    {
        var validation = _validationService.Validate(request.JsonContent);

        if (!validation.IsValid)
        {
            // Important : on ne tente pas d'enregistrer ce contenu en base.
            // FlowImport.RawJson est une colonne PostgreSQL de type "jsonb" :
            // si on essayait d'y insérer un contenu qui n'est pas du JSON valide
            // (fichier non-JSON, texte quelconque...), PostgreSQL rejetterait
            // l'insertion avec une erreur bas niveau, non interceptée, qui
            // remontait comme une erreur 500 générique au lieu du vrai message
            // métier. On renvoie donc directement l'erreur de validation, sans
            // toucher à la base.
            var rejected = new FlowImportResultDto(
                Guid.Empty,
                string.IsNullOrWhiteSpace(request.FileName) ? "Flux sans nom" : request.FileName,
                false,
                validation.Error,
                0,
                DateTime.UtcNow);
            return UnprocessableEntity(rejected);
        }

        var flowImport = new FlowImport
        {
            Name = string.IsNullOrWhiteSpace(request.FileName) ? "Flux sans nom" : request.FileName,
            RawJson = request.JsonContent,
            IsValid = true,
            ValidationError = null,
            ImportedByUserId = User.GetUserId()
        };

        try
        {
            var parsed = _parserService.Parse(request.JsonContent);
            flowImport.ActionsCount = parsed.Nodes.Count;
            flowImport.Name = string.IsNullOrWhiteSpace(request.FileName) ? parsed.FlowName : request.FileName;
        }
        catch
        {
            // La validation basique a réussi mais le parsing détaillé a échoué :
            // on conserve quand même l'import, il pourra être ré-analysé plus tard.
            flowImport.ActionsCount = 0;
        }

        _db.FlowImports.Add(flowImport);
        await _db.SaveChangesAsync(ct);

        var result = new FlowImportResultDto(
            flowImport.Id, flowImport.Name, flowImport.IsValid, flowImport.ValidationError,
            flowImport.ActionsCount, flowImport.ImportedAtUtc);

        return Ok(result);
    }

    /// <summary>
    /// Importe la définition d'un flux cloud existant depuis Power Platform, sans
    /// téléchargement manuel d'un fichier JSON. Les deux jetons délégués ne sont
    /// utilisés que pendant la requête, puis immédiatement oubliés.
    /// </summary>
    [HttpPost("import/power-platform")]
    public async Task<ActionResult<FlowImportResultDto>> ImportFromPowerPlatform(
        PowerPlatformFlowImportRequestDto request, CancellationToken ct)
    {
        var definition = await _powerPlatformFlowService.GetFlowDefinitionAsync(
            GetRequiredToken("X-PowerPlatform-Access-Token"),
            GetRequiredToken("X-Dataverse-Access-Token"),
            request.EnvironmentId,
            request.WorkflowId,
            ct);

        var validation = _validationService.Validate(definition.DefinitionJson);
        if (!validation.IsValid)
            return UnprocessableEntity(new { message = validation.Error });

        var parsed = _parserService.Parse(definition.DefinitionJson);
        var flowImport = new FlowImport
        {
            Name = string.IsNullOrWhiteSpace(parsed.FlowName) || parsed.FlowName == "Flux sans nom"
                ? definition.DisplayName
                : parsed.FlowName,
            RawJson = definition.DefinitionJson,
            IsValid = true,
            ActionsCount = parsed.Nodes.Count,
            ImportedByUserId = User.GetUserId(),
            Source = FlowImportSource.PowerPlatform,
            PowerPlatformEnvironmentId = definition.EnvironmentId,
            PowerPlatformTenantId = definition.TenantId,
            PowerPlatformWorkflowId = request.WorkflowId
        };

        _db.FlowImports.Add(flowImport);
        await _db.SaveChangesAsync(ct);

        return Ok(new FlowImportResultDto(
            flowImport.Id, flowImport.Name, flowImport.IsValid, flowImport.ValidationError,
            flowImport.ActionsCount, flowImport.ImportedAtUtc));
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

    private string GetRequiredToken(string headerName)
    {
        var token = Request.Headers[headerName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
            throw new BusinessException("Connectez-vous à Microsoft 365 pour accéder aux flux Power Automate.");

        return token;
    }
}
