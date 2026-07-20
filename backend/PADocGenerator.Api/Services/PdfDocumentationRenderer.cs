using PADocGenerator.Api.Models.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Génération PDF pour le module d'export (section 6). Utilise QuestPDF
/// (licence Community gratuite pour ce contexte académique/interne).
/// </summary>
public class PdfDocumentationRenderer
{
    static PdfDocumentationRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(DocumentationDetailDto documentation)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(documentation.Title).FontSize(20).Bold();
                    col.Item().Text($"Flux source : {documentation.FlowName}").FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Statut : {documentation.Status}  •  Version : v{documentation.CurrentVersionNumber}  •  Mis à jour le {documentation.UpdatedAtUtc:dd/MM/yyyy HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Text("Résumé fonctionnel").FontSize(15).Bold();
                    col.Item().Text(documentation.Content.FunctionalSummary);

                    if (documentation.Content.Steps.Count > 0)
                    {
                        col.Item().PaddingTop(6).Text("Étapes du flux").FontSize(15).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(6);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Étape");
                                header.Cell().Element(HeaderCell).Text("Description");
                                header.Cell().Element(HeaderCell).Text("Clé");
                            });

                            foreach (var step in documentation.Content.Steps)
                            {
                                if (step.IsImportant)
                                {
                                    table.Cell().Element(BodyCell).Text(step.StepName).Bold();
                                }
                                else
                                {
                                    table.Cell().Element(BodyCell).Text(step.StepName);
                                }

                                table.Cell().Element(BodyCell).Text(step.Description);
                                table.Cell().Element(BodyCell).Text(step.IsImportant ? "★" : "");
                            }

                            static IContainer HeaderCell(IContainer c) => c.DefaultTextStyle(x => x.Bold())
                                .PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                            static IContainer BodyCell(IContainer c) => c.PaddingVertical(4)
                                .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                        });
                    }

                    if (documentation.Content.Dependencies.Count > 0)
                    {
                        col.Item().PaddingTop(6).Text("Dépendances entre actions, conditions et variables").FontSize(15).Bold();
                        foreach (var dep in documentation.Content.Dependencies)
                        {
                            col.Item().Text(text =>
                            {
                                text.Span($"{dep.From} → {dep.To} : ").Bold();
                                text.Span(dep.ExplanationText);
                            });
                        }
                    }

                    if (documentation.Content.ImportantSteps.Count > 0)
                    {
                        col.Item().PaddingTop(6).Text("Étapes importantes à retenir").FontSize(15).Bold();
                        foreach (var stepName in documentation.Content.ImportantSteps)
                        {
                            col.Item().Text($"•  {stepName}");
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Généré par la plateforme Générateur de documentation IA pour Power Automate — page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
