using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Data;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Controllers;

/// <summary>
/// Regroupe le module de génération, le module de mise en forme et le module
/// de gestion documentaire (section 6) : lancement de la génération IA,
/// consultation, modification avant enregistrement, changement de statut,
/// historique de versions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFlowParserService _flowParserService;
    private readonly IAiDocumentationService _aiDocumentationService;
    private readonly IDocumentFormattingService _formattingService;
    private readonly IDocumentManagementService _managementService;
    private readonly ISearchService _searchService;

    public DocumentationController(
        AppDbContext db,
        IFlowParserService flowParserService,
        IAiDocumentationService aiDocumentationService,
        IDocumentFormattingService formattingService,
        IDocumentManagementService managementService,
        ISearchService searchService)
    {
        _db = db;
        _flowParserService = flowParserService;
        _aiDocumentationService = aiDocumentationService;
        _formattingService = formattingService;
        _managementService = managementService;
        _searchService = searchService;
    }

    /// <summary>
    /// Lance la génération de documentation depuis l'interface (section 4) :
    /// lecture/préparation -> génération IA -> mise en forme -> enregistrement
    /// (statut initial : Brouillon).
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<DocumentationDetailDto>> Generate(GenerateDocumentationRequestDto request, CancellationToken ct)
    {
        var flowImport = await _db.FlowImports.FindAsync([request.FlowImportId], ct);
        if (flowImport is null)
            return NotFound(new { message = UserMessages.FlowImportNotFound });

        if (!flowImport.IsValid)
            return UnprocessableEntity(new { message = UserMessages.InvalidFlowForDocumentation });

        var parsedFlow = _flowParserService.Parse(flowImport.RawJson);

        var rawContent = await _aiDocumentationService.GenerateAsync(parsedFlow, ct);

        var contentWithDiagram = rawContent with
        {
            Diagram = new DocumentationDiagramDto(
                parsedFlow.Nodes.Select(node =>
                    new DocumentationDiagramNodeDto(
                        node.Id,
                        node.Name,
                        node.Type,
                        node.NodeType)).ToList(),

                parsedFlow.Edges.Select(edge =>
                    new DocumentationDiagramEdgeDto(
                        edge.SourceId,
                        edge.TargetId,
                        edge.Label)).ToList())
        };

        var formattedContent = _formattingService.Format(contentWithDiagram);

        var documentation = await _managementService.CreateFromGenerationAsync(
            flowImport.Id, User.GetUserId(), formattedContent, ct);

        return Ok(documentation);
    }

    /// <summary>
    /// Relance la génération IA sur le flux d'origine et enregistre le résultat
    /// comme nouvelle version de la MÊME documentation (section 4 : "Permettre
    /// la régénération" - backlog, feature "Résumé fonctionnel"). Utile quand la
    /// première génération est insatisfaisante, sans avoir à ré-importer le flux.
    /// </summary>
    [HttpPost("{id:guid}/regenerate")]
    public async Task<ActionResult<DocumentationDetailDto>> Regenerate(Guid id, CancellationToken ct)
    {
        var existing = await _managementService.GetByIdAsync(id, ct);
        if (existing is null)
            return NotFound(new { message = UserMessages.DocumentationNotFound });

        if (!User.CanModify(existing.CreatedByUserId))
            return Forbid();

        var flowImport = await _db.FlowImports.FindAsync([existing.FlowImportId], ct);
        if (flowImport is null || !flowImport.IsValid)
            return UnprocessableEntity(new { message = UserMessages.InvalidOriginFlow });

        var parsedFlow = _flowParserService.Parse(flowImport.RawJson);
        
        var rawContent = await _aiDocumentationService.GenerateAsync(parsedFlow, ct);

        var contentWithDiagram = rawContent with
        {
            Diagram = new DocumentationDiagramDto(
                parsedFlow.Nodes.Select(node =>
                    new DocumentationDiagramNodeDto(
                        node.Id,
                        node.Name,
                        node.Type,
                        node.NodeType)).ToList(),

                parsedFlow.Edges.Select(edge =>
                    new DocumentationDiagramEdgeDto(
                        edge.SourceId,
                        edge.TargetId,
                        edge.Label)).ToList())
        };

        var formattedContent = _formattingService.Format(contentWithDiagram);

        var updated = await _managementService.UpdateAsync(
            id, User.GetUserId(),
            new UpdateDocumentationDto(existing.Title, formattedContent, "Régénération via IA."),
            false, ct);

        return Ok(updated);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentationDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var documentation = await _managementService.GetByIdAsync(id, ct);
        if (documentation is null)
            return NotFound(new { message = UserMessages.DocumentationNotFound });

        if (!User.CanModify(documentation.CreatedByUserId))
            return Forbid();

        return Ok(documentation);
    }

    /// <summary>Modification de la documentation générée avant enregistrement définitif
    /// (section 4). Crée une nouvelle version et conserve l'historique.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentationDetailDto>> Update(Guid id, UpdateDocumentationDto dto, CancellationToken ct)
    {
        var existing = await _managementService.GetByIdAsync(id, ct);
        if (existing is null)
            return NotFound(new { message = UserMessages.DocumentationNotFound });

        if (!User.CanModify(existing.CreatedByUserId))
            return Forbid();

        var updated = await _managementService.UpdateAsync(id, User.GetUserId(), dto, true, ct);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<DocumentationDetailDto>> ChangeStatus(Guid id, ChangeStatusDto dto, CancellationToken ct)
    {
        var existing = await _managementService.GetByIdAsync(id, ct);
        if (existing is null) return NotFound(new { message = UserMessages.DocumentationNotFound });

        if (!User.CanModify(existing.CreatedByUserId))
            return Forbid();

        try
        {
            var updated = await _managementService.ChangeStatusAsync(id, dto.NewStatus, ct);
            return Ok(updated);
        }
        catch (BusinessException)
        {
            return BadRequest(new { message = UserMessages.InvalidStatus });
        }
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<List<DocumentationVersionSummaryDto>>> GetVersions(Guid id, CancellationToken ct)
    {
        var existing = await _managementService.GetByIdAsync(id, ct);
        if (existing is null)
            return NotFound(new { message = UserMessages.DocumentationNotFound });

        if (!User.CanModify(existing.CreatedByUserId))
            return Forbid();

        return Ok(await _managementService.GetVersionHistoryAsync(id, ct));
    }

    [HttpGet("{id:guid}/versions/{versionNumber:int}")]
    public async Task<ActionResult<DocumentationVersionDetailDto>> GetVersion(Guid id, int versionNumber, CancellationToken ct)
    {
        var existing = await _managementService.GetByIdAsync(id, ct);
        if (existing is null)
            return NotFound(new { message = UserMessages.DocumentationNotFound });

        if (!User.CanModify(existing.CreatedByUserId))
            return Forbid();

        try
        {
            return Ok(await _managementService.GetVersionAsync(id, versionNumber, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = UserMessages.VersionNotFound });
        }
    }

    /// <summary>Module de recherche et consultation (section 6) : recherche par
    /// mot-clé, nom de flux ou statut.</summary>
    [HttpGet("search")]
    public async Task<ActionResult> Search([FromQuery] SearchDocumentationQueryDto query, CancellationToken ct)
    {
        var (items, totalCount) = await _searchService.SearchAsync(
            query, User.GetUserId(), User.IsInRole("Administrateur"), ct);
        return Ok(new { items, totalCount, page = query.Page, pageSize = query.PageSize });
    }

    /// <summary>Suppression définitive - fonction sensible réservée aux administrateurs
    /// (section 6, module de gestion des rôles).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrateur")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _managementService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = UserMessages.DocumentationNotFound });
        }
    }
}
