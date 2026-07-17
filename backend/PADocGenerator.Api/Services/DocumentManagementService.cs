using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
            ?? throw new KeyNotFoundException($"Flux importé introuvable : {flowImportId}");

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

        return MapToDetailDto(documentation, flow.Name, version);
    }

    public async Task<DocumentationDetailDto?> GetByIdAsync(Guid documentationId, CancellationToken ct = default)
    {
        var documentation = await _db.Documentations
            .Include(d => d.FlowImport)
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentationId, ct);

        if (documentation is null) return null;

        var currentVersion = documentation.Versions.First(v => v.VersionNumber == documentation.CurrentVersionNumber);
        return MapToDetailDto(documentation, documentation.FlowImport?.Name ?? "Flux inconnu", currentVersion);
    }

    public async Task<DocumentationDetailDto> UpdateAsync(
        Guid documentationId, Guid editedByUserId, UpdateDocumentationDto dto, CancellationToken ct = default)
    {
        var documentation = await _db.Documentations
            .Include(d => d.FlowImport)
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentationId, ct)
            ?? throw new KeyNotFoundException($"Documentation introuvable : {documentationId}");

        // Section 4 : "Possibilité de modifier la documentation générée avant enregistrement"
        // + "Conservation ... de l'historique de versions" -> chaque modification crée une
        // nouvelle version plutôt que d'écraser la précédente.
        var nextVersionNumber = documentation.Versions.Max(v => v.VersionNumber) + 1;

        var newVersion = new DocumentationVersion
        {
            DocumentationId = documentation.Id,
            VersionNumber = nextVersionNumber,
            FunctionalSummary = dto.Content.FunctionalSummary,
            StructuredContentJson = JsonSerializer.Serialize(dto.Content, JsonOptions),
            IsManuallyEdited = true,
            EditedByUserId = editedByUserId,
            ChangeNote = dto.ChangeNote
        };

        documentation.Versions.Add(newVersion);
        documentation.Title = dto.Title;
        documentation.CurrentVersionNumber = nextVersionNumber;
        documentation.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return MapToDetailDto(documentation, documentation.FlowImport?.Name ?? "Flux inconnu", newVersion);
    }

    public async Task<DocumentationDetailDto> ChangeStatusAsync(
        Guid documentationId, string newStatus, CancellationToken ct = default)
    {
        if (!Enum.TryParse<DocumentationStatus>(newStatus, ignoreCase: true, out var parsedStatus))
        {
            throw new ArgumentException(
                $"Statut invalide '{newStatus}'. Valeurs autorisées : Brouillon, Valide, Archive.");
        }

        var documentation = await _db.Documentations
            .Include(d => d.FlowImport)
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentationId, ct)
            ?? throw new KeyNotFoundException($"Documentation introuvable : {documentationId}");

        documentation.Status = parsedStatus;
        documentation.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var currentVersion = documentation.Versions.First(v => v.VersionNumber == documentation.CurrentVersionNumber);
        return MapToDetailDto(documentation, documentation.FlowImport?.Name ?? "Flux inconnu", currentVersion);
    }

    public async Task<List<DocumentationVersionSummaryDto>> GetVersionHistoryAsync(
        Guid documentationId, CancellationToken ct = default)
    {
        return await _db.DocumentationVersions
            .Include(v => v.EditedByUser)
            .Where(v => v.DocumentationId == documentationId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new DocumentationVersionSummaryDto(
                v.VersionNumber,
                v.IsManuallyEdited,
                v.EditedByUser != null ? v.EditedByUser.FullName : "Inconnu",
                v.CreatedAtUtc,
                v.ChangeNote))
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid documentationId, CancellationToken ct = default)
    {
        // Réservé au rôle Administrateur - cf. section 6, module de gestion des rôles :
        // "Protège les fonctions sensibles (telles que la suppression définitive de
        // documents ...)". Le contrôle d'accès est appliqué au niveau du Controller.
        var documentation = await _db.Documentations.FirstOrDefaultAsync(d => d.Id == documentationId, ct)
            ?? throw new KeyNotFoundException($"Documentation introuvable : {documentationId}");

        _db.Documentations.Remove(documentation);
        await _db.SaveChangesAsync(ct);
    }

    private static DocumentationDetailDto MapToDetailDto(Documentation documentation, string flowName, DocumentationVersion version)
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
            documentation.CreatedAtUtc,
            documentation.UpdatedAtUtc);
    }
}
