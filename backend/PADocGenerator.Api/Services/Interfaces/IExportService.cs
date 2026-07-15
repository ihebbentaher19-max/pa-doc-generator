using PADocGenerator.Api.Models.Dtos;

namespace PADocGenerator.Api.Services.Interfaces;

public enum ExportFormat { Pdf, Word }

/// <summary>
/// Module d'export (section 6) : génère des fichiers téléchargeables
/// Word ou PDF pour partage hors plateforme / archivage.
/// </summary>
public interface IExportService
{
    Task<(byte[] Content, string FileName, string ContentType)> ExportAsync(DocumentationDetailDto documentation, ExportFormat format, CancellationToken ct = default);
}
