using PADocGenerator.Api.Models.Dtos;

namespace PADocGenerator.Api.Services.Interfaces;

/// <summary>
/// Module de gestion documentaire (section 6) : enregistrement, métadonnées,
/// historique de versions, statut (brouillon/validé/archivé).
/// </summary>
public interface IDocumentManagementService
{
    Task<DocumentationDetailDto> CreateFromGenerationAsync(Guid flowImportId, Guid createdByUserId, DocumentationContentDto content, CancellationToken ct = default);
    Task<DocumentationDetailDto?> GetByIdAsync(Guid documentationId, CancellationToken ct = default);
    Task<DocumentationDetailDto> UpdateAsync(Guid documentationId, Guid editedByUserId, UpdateDocumentationDto dto, bool isManuallyEdited = true, CancellationToken ct = default);
    Task<DocumentationVersionDetailDto> GetVersionAsync(Guid documentationId, int versionNumber, CancellationToken ct = default);
    Task<DocumentationDetailDto> ChangeStatusAsync(Guid documentationId, string newStatus, CancellationToken ct = default);
    Task<List<DocumentationVersionSummaryDto>> GetVersionHistoryAsync(Guid documentationId, CancellationToken ct = default);
    Task DeleteAsync(Guid documentationId, CancellationToken ct = default);
}
