using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PADocGenerator.Api.Models.Dtos;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Génération Word (.docx) pour le module d'export (section 6), via
/// DocumentFormat.OpenXml (aucune dépendance vers Word/Office installé,
/// donc compatible avec un déploiement serveur Azure).
/// </summary>
public class WordDocumentationRenderer
{
    public byte[] Render(DocumentationDetailDto documentation)
    {
        using var stream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            body.AppendChild(CreateHeading(documentation.Title, 1));
            body.AppendChild(CreateParagraph(
                $"Flux source : {documentation.FlowName}    |    Statut : {documentation.Status}    |    Version : v{documentation.CurrentVersionNumber}",
                italic: true));
            body.AppendChild(CreateParagraph(
                $"Mis à jour le {documentation.UpdatedAtUtc:dd/MM/yyyy HH:mm}", italic: true, small: true));

            body.AppendChild(CreateHeading("Résumé fonctionnel", 2));
            body.AppendChild(CreateParagraph(documentation.Content.FunctionalSummary));

            if (documentation.Content.Steps.Count > 0)
            {
                body.AppendChild(CreateHeading("Étapes du flux", 2));
                body.AppendChild(CreateStepsTable(documentation.Content.Steps));
            }

            if (documentation.Content.Dependencies.Count > 0)
            {
                body.AppendChild(CreateHeading("Dépendances entre actions, conditions et variables", 2));
                foreach (var dep in documentation.Content.Dependencies)
                {
                    body.AppendChild(CreateParagraph($"{dep.From} → {dep.To} : {dep.ExplanationText}", bullet: true));
                }
            }

            if (documentation.Content.ImportantSteps.Count > 0)
            {
                body.AppendChild(CreateHeading("Étapes importantes à retenir", 2));
                foreach (var stepName in documentation.Content.ImportantSteps)
                {
                    body.AppendChild(CreateParagraph(stepName, bullet: true));
                }
            }

            body.AppendChild(new SectionProperties(new PageSize { Width = 11906, Height = 16838 }));
        }

        return stream.ToArray();
    }

    private static Paragraph CreateHeading(string text, int level)
    {
        var styleId = level == 1 ? "Heading1" : "Heading2";
        return new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
            new Run(new RunProperties(new Bold(), new FontSize { Val = level == 1 ? "32" : "26" }),
                new Text(text)));
    }

    private static Paragraph CreateParagraph(string text, bool italic = false, bool bullet = false, bool small = false)
    {
        var runProperties = new RunProperties();
        if (italic) runProperties.Append(new Italic());
        if (small) runProperties.Append(new FontSize { Val = "18" });

        var paragraphProperties = new ParagraphProperties();
        if (bullet)
        {
            paragraphProperties.Append(new NumberingProperties(
                new NumberingLevelReference { Val = 0 },
                new NumberingId { Val = 1 }));
        }

        return new Paragraph(
            paragraphProperties,
            new Run(runProperties, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Table CreateStepsTable(List<DocumentationStepDto> steps)
    {
        var table = new Table();

        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6 },
                new BottomBorder { Val = BorderValues.Single, Size = 6 },
                new LeftBorder { Val = BorderValues.Single, Size = 6 },
                new RightBorder { Val = BorderValues.Single, Size = 6 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }));
        table.AppendChild(tableProperties);

        table.AppendChild(CreateTableRow(new[] { "Étape", "Description", "Importante" }, isHeader: true));

        foreach (var step in steps)
        {
            table.AppendChild(CreateTableRow(new[] { step.StepName, step.Description, step.IsImportant ? "Oui" : "" }));
        }

        return table;
    }

    private static TableRow CreateTableRow(string[] cellsText, bool isHeader = false)
    {
        var row = new TableRow();
        foreach (var cellText in cellsText)
        {
            var runProps = new RunProperties();
            if (isHeader) runProps.Append(new Bold());

            var cell = new TableCell(
                new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto }),
                new Paragraph(new Run(runProps, new Text(cellText))));
            row.AppendChild(cell);
        }
        return row;
    }
}
