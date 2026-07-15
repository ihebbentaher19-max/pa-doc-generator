using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Façade du module d'export (section 6) : délègue vers le moteur PDF
/// (QuestPDF) ou Word (OpenXml) selon le format demandé.
/// </summary>
public class ExportService : IExportService
{
    private readonly PdfDocumentationRenderer _pdfRenderer;
    private readonly WordDocumentationRenderer _wordRenderer;

    public ExportService(PdfDocumentationRenderer pdfRenderer, WordDocumentationRenderer wordRenderer)
    {
        _pdfRenderer = pdfRenderer;
        _wordRenderer = wordRenderer;
    }

    public Task<(byte[] Content, string FileName, string ContentType)> ExportAsync(
        DocumentationDetailDto documentation, ExportFormat format, CancellationToken ct = default)
    {
        var safeTitle = string.Join("_", documentation.Title.Split(Path.GetInvalidFileNameChars()));

        (byte[] Content, string FileName, string ContentType) result = format switch
        {
            ExportFormat.Pdf => (
                _pdfRenderer.Render(documentation),
                $"{safeTitle}.pdf",
                "application/pdf"),

            ExportFormat.Word => (
                _wordRenderer.Render(documentation),
                $"{safeTitle}.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),

            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Format d'export non supporté.")
        };

        return Task.FromResult(result);
    }
}
