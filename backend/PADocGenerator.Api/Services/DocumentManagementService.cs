using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Data;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Models.Entities;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Implémentation du module de gestion documentaire (section 6) :
/// enregistrement des flux et documentations, métadonnées, statut
/// (brouillon/validé/archivé) et historique de versions.
/// </summary>
public class DocumentManagementService : IDocumentManagementService
{
    private readonly AppDbContext _db;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DocumentManagementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DocumentationDetailDto> CreateFromGenerationAsync(
        Guid flowImportId, Guid createdByUserId, DocumentationContentDto content, CancellationToken ct = default)
    {
        var flow = await _db.FlowImports.FirstOrDefaultAsync(f => f.Id == flowImportId, ct)
            ?? throw new KeyNotFoundException(UserMessages.FlowImportNotFound);

        var creator = await _db.Users.FindAsync([createdByUserId], ct);

        var documentation = new Documentation
        {
            FlowImportId = flowImportId,
            Title = $"Documentation - {flow.Name}",
            Status = DocumentationStatus.Brouillon,
            CreatedByUserId = createdByUserId,
            CurrentVersionNumber = 1
        };

        var version = new DocumentationVersion
        {
            DocumentationId = documentation.Id,
            VersionNumber = 1,
            FunctionalSummary = content.FunctionalSummary,
            StructuredContentJson = JsonSerializer.Serialize(content, JsonOptions),
            IsManuallyEdited = false,
            EditedByUserId = createdByUserId,
            ChangeNote = "Génération initiale par le modèle d'IA."
        };

        documentation.Versions.Add(version);

        _db.Documentations.Add(documentation);
        await _db.SaveChangesAsync(ct);

        return MapToDetailDto(documentation, flow.Name, version, creator?.FullName ?? "Inconnu");
    }

    public async Task<DocumentationDetailDto?> GetByIdAsync(Guid documentationId, CancellationToken ct = default)
    {
        // AsNoTracking() est essentiel ici : GetByIdAsync est aussi utilisé comme
        // pré-vérification (propriétaire/admin) avant Update/ChangeStatus/Regenerate,
        // sur le même DbContext (scoped par requête). Sans AsNoTracking, cette lecture
        // enregistrait déjà l'entité (et sa collection Versions) dans le suivi des
        // modifications d'EF Core ; la requête de la méthode de mutation suivante
        // rechargeait ensuite le même graphe par-dessus, ce qui corrompait le suivi
        // des changements et provoquait un DbUpdateConcurrencyException ("0 rows
        // affected") au moment du SaveChangesAsync, même si la ligne existait bien.
        var documentation = await _db.Documentations
            .AsNoTracking()
            .Include(d => d.FlowImport)
            .Include(d => d.Versions)
            .Include(d => d.CreatedByUser)
            .FirstOrDefaultAsync(d => d.Id == documentationId, ct);

        if (documentation is null) return null;

        var currentVersion = documentation.Versions.First(v => v.VersionNumber == documentation.CurrentVersionNumber);
        return MapToDetailDto(
            documentation, documentation.FlowImport?.Name ?? "Flux inconnu", currentVersion,
            documentation.CreatedByUser?.FullName ?? "Inconnu");
    }

    public async Task<DocumentationDetailDto> UpdateAsync(
        Guid documentationId, Guid editedByUserId, UpdateDocumentationDto dto, bool isManuallyEdited = true, CancellationToken ct = default)
    {
        var documentation = await _db.Documentations
            .Include(d => d.FlowImport)
            .Include(d => d.Versions)
            .Include(d => d.CreatedByUser)
            .FirstOrDefaultAsync(d => d.Id == documentationId, ct)
            ?? throw new KeyNotFoundException(UserMessages.DocumentationNotFound);

        // Section 4 : "Possibilité de modifier la documentation générée avant enregistrement"
        // + "Conservation ... de l'historique de versions" -> chaque modification crée une
        // nouvelle version plutôt que d'écraser la précédente.
        var versions = documentation.Versions ?? new List<DocumentationVersion>();
        if (!versions.Any())
        {
            throw new BusinessException(UserMessages.ActiveVersionNotFound);
        }

        var nextVersionNumber = versions.Max(v => v.VersionNumber) + 1;

        var newVersion = new DocumentationVersion
        {
            DocumentationId = documentation.Id,
            Documentation = documentation,
            VersionNumber = nextVersionNumber,
            FunctionalSummary = dto.Content.FunctionalSummary,
            StructuredContentJson = JsonSerializer.Serialize(dto.Content, JsonOptions),
            IsManuallyEdited = isManuallyEdited,
            EditedByUserId = editedByUserId,
            ChangeNote = dto.ChangeNote
        };

        _db.DocumentationVersions.Add(newVersion);
        documentation.Title = dto.Title;
        documentation.CurrentVersionNumber = nextVersionNumber;
        documentation.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return MapToDetailDto(
            documentation, documentation.FlowImport?.Name ?? "Flux inconnu", newVersion,
            documentation.CreatedByUser?.FullName ?? "Inconnu");
    }

    public async Task<DocumentationVersionDetailDto> GetVersionAsync(
        Guid documentationId, int versionNumber, CancellationToken ct = default)
    {
        var version = await _db.DocumentationVersions
            .Include(v => v.EditedByUser)
            .Include(v => v.Documentation!)
            .ThenInclude(d => d.FlowImport!)
            .FirstOrDefaultAsync(v => v.DocumentationId == documentationId && v.VersionNumber == versionNumber, ct)
            ?? throw new KeyNotFoundException(UserMessages.VersionNotFound);

        var content = JsonSerializer.Deserialize<DocumentationContentDto>(version.StructuredContentJson, JsonOptions)
            ?? new DocumentationContentDto(version.FunctionalSummary, new(), new(), new());

        return new DocumentationVersionDetailDto(
            documentationId,
            version.VersionNumber,
            version.IsManuallyEdited,
            version.EditedByUser?.FullName ?? "Inconnu",
            version.CreatedAtUtc,
            version.ChangeNote,
            content);
    }

    public async Task<DocumentationDetailDto> ChangeStatusAsync(
        Guid documentationId, string newStatus, CancellationToken ct = default)
    {
        if (!Enum.TryParse<DocumentationStatus>(newStatus, ignoreCase: true, out var parsedStatus))
        {
            throw new ArgumentException(UserMessages.InvalidStatus);
        }

        var documentation = await _db.Documentations
            .Include(d => d.FlowImport)
            .Include(d => d.Versions)
            .Include(d => d.CreatedByUser)
            .FirstOrDefaultAsync(d => d.Id == documentationId, ct)
            ?? throw new KeyNotFoundException(UserMessages.DocumentationNotFound);

        documentation.Status = parsedStatus;
        documentation.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var currentVersion = documentation.Versions?.FirstOrDefault(v => v.VersionNumber == documentation.CurrentVersionNumber)
            ?? throw new BusinessException(UserMessages.ActiveVersionNotFound);
        return MapToDetailDto(
            documentation, documentation.FlowImport?.Name ?? "Flux inconnu", currentVersion,
            documentation.CreatedByUser?.FullName ?? "Inconnu");
    }

    public async Task<List<DocumentationVersionSummaryDto>> GetVersionHistoryAsync(
        Guid documentationId, CancellationToken ct = default)
    {
        var versions = await _db.DocumentationVersions
            .Include(v => v.EditedByUser)
            .Where(v => v.DocumentationId == documentationId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        return versions.Select(v => new DocumentationVersionSummaryDto(
            v.VersionNumber,
            v.IsManuallyEdited,
            v.EditedByUser != null ? v.EditedByUser.FullName : "Inconnu",
            v.CreatedAtUtc,
            v.ChangeNote,
            JsonSerializer.Deserialize<DocumentationContentDto>(v.StructuredContentJson, JsonOptions)
                ?? new DocumentationContentDto(v.FunctionalSummary, new(), new(), new())
        )).ToList();
    }

    public async Task DeleteAsync(Guid documentationId, CancellationToken ct = default)
    {
        // Réservé au rôle Administrateur - cf. section 6, module de gestion des rôles :
        // "Protège les fonctions sensibles (telles que la suppression définitive de
        // documents ...)". Le contrôle d'accès est appliqué au niveau du Controller.
        var documentation = await _db.Documentations.FirstOrDefaultAsync(d => d.Id == documentationId, ct)
            ?? throw new KeyNotFoundException(UserMessages.DocumentationNotFound);

        _db.Documentations.Remove(documentation);
        await _db.SaveChangesAsync(ct);
    }

    private static DocumentationDetailDto MapToDetailDto(
        Documentation documentation, string flowName, DocumentationVersion version, string createdByUserName)
    {
        var content = JsonSerializer.Deserialize<DocumentationContentDto>(version.StructuredContentJson, JsonOptions)
            ?? new DocumentationContentDto(version.FunctionalSummary, new(), new(), new());

        return new DocumentationDetailDto(
            documentation.Id,
            documentation.Title,
            flowName,
            documentation.FlowImportId,
            documentation.Status.ToString(),
            documentation.CurrentVersionNumber,
            content,
            documentation.CreatedByUserId,
            createdByUserName,
            documentation.CreatedAtUtc,
            documentation.UpdatedAtUtc);
    }
}