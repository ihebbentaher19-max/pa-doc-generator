using PADocGenerator.Api.Models.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Génération PDF de la documentation Power Automate.
/// Inclut le résumé fonctionnel, les étapes, les dépendances
/// et le diagramme technique du flux.
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

                page.DefaultTextStyle(x =>
                    x.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(col =>
                {
                    col.Item().Text(documentation.Title)
                        .FontSize(20)
                        .Bold();

                    col.Item()
                        .Text($"Flux source : {documentation.FlowName}")
                        .FontSize(11)
                        .FontColor(Colors.Grey.Darken1);

                    col.Item()
                        .Text(
                            $"Statut : {documentation.Status}  •  " +
                            $"Version : v{documentation.CurrentVersionNumber}  •  " +
                            $"Mis à jour le {documentation.UpdatedAtUtc:dd/MM/yyyy HH:mm}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);

                    col.Item()
                        .PaddingTop(8)
                        .LineHorizontal(1)
                        .LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Spacing(12);

                    // =========================
                    // Résumé fonctionnel
                    // =========================
                    col.Item()
                        .Text("Résumé fonctionnel")
                        .FontSize(15)
                        .Bold();

                    col.Item()
                        .Text(documentation.Content.FunctionalSummary ?? string.Empty);

                    // =========================
                    // Étapes du flux
                    // =========================
                    if (documentation.Content.Steps?.Count > 0)
                    {
                        col.Item()
                            .PaddingTop(6)
                            .Text("Étapes du flux")
                            .FontSize(15)
                            .Bold();

                        foreach (var step in documentation.Content.Steps)
                        {
                            col.Item()
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(10)
                                .Column(stepColumn =>
                                {
                                    stepColumn.Spacing(5);

                                    stepColumn.Item().Text(text =>
                                    {
                                        text.Span(step.StepName).Bold();
                                        text.Span($" — {step.StepType}");
                                    });

                                    stepColumn.Item().Text(text =>
                                    {
                                        text.Span("Description : ").Bold();
                                        text.Span(step.Description ?? string.Empty);
                                    });

                                    if (!string.IsNullOrWhiteSpace(step.Purpose))
                                    {
                                        stepColumn.Item().Text(text =>
                                        {
                                            text.Span("Objectif : ").Bold();
                                            text.Span(step.Purpose);
                                        });
                                    }

                                    if (step.Variables?.Count > 0)
                                    {
                                        stepColumn.Item()
                                            .PaddingTop(3)
                                            .Text("Variables ou données utilisées :")
                                            .Bold();

                                        foreach (var variable in step.Variables)
                                        {
                                            stepColumn.Item().Text(text =>
                                            {
                                                text.Span($"• {variable.Name}").Bold();

                                                if (!string.IsNullOrWhiteSpace(variable.Value))
                                                {
                                                    text.Span($" = {variable.Value}");
                                                }

                                                if (!string.IsNullOrWhiteSpace(variable.Description))
                                                {
                                                    text.Span($" — {variable.Description}");
                                                }
                                            });
                                        }
                                    }

                                    if (step.Inputs?.Count > 0)
                                    {
                                        stepColumn.Item()
                                            .PaddingTop(3)
                                            .Text("Entrées / paramètres :")
                                            .Bold();

                                        foreach (var input in step.Inputs)
                                        {
                                            stepColumn.Item()
                                                .Text($"• {input.Key} : {input.Value}");
                                        }
                                    }
                                });
                        }
                    }

                    // =========================
                    // Dépendances
                    // =========================
                    if (documentation.Content.Dependencies?.Count > 0)
                    {
                        col.Item()
                            .PaddingTop(6)
                            .Text("Dépendances entre actions, conditions et variables")
                            .FontSize(15)
                            .Bold();

                        foreach (var dep in documentation.Content.Dependencies)
                        {
                            col.Item().Text(text =>
                            {
                                text.Span($"{dep.From} → {dep.To} : ").Bold();
                                text.Span(dep.ExplanationText ?? string.Empty);
                            });
                        }
                    }

                    // =========================
                    // Diagramme technique
                    // =========================
                    if (documentation.Content.Diagram?.Nodes?.Count > 0)
                    {
                        col.Item()
                            .PageBreak();

                        col.Item()
                            .Text("Diagramme du flux")
                            .FontSize(15)
                            .Bold();

                        col.Item()
                            .Text(
                                "Représentation technique des étapes et de leurs relations.")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        AddDiagram(
                            col,
                            documentation.Content.Diagram);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span(
                        "Généré par la plateforme Générateur de documentation IA pour Power Automate — page ");

                    text.CurrentPageNumber();

                    text.Span(" / ");

                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void AddDiagram(
        ColumnDescriptor column,
        DocumentationDiagramDto diagram)
    {
        var edges = diagram.Edges ?? new List<DocumentationDiagramEdgeDto>();

        var nodesById = diagram.Nodes.ToDictionary(node => node.Id);

        // Nombre de connexions entrantes pour identifier les nœuds racines.
        var incomingCount = diagram.Nodes.ToDictionary(
            node => node.Id,
            _ => 0);

        foreach (var edge in edges)
        {
            if (incomingCount.ContainsKey(edge.TargetId))
            {
                incomingCount[edge.TargetId]++;
            }
        }

        // Construction des niveaux du diagramme.
        var levels = new List<List<DocumentationDiagramNodeDto>>();
        var assignedLevels = new Dictionary<string, int>();
        var queue = new Queue<string>();

        foreach (var node in diagram.Nodes
                     .Where(node => incomingCount[node.Id] == 0))
        {
            assignedLevels[node.Id] = 0;
            queue.Enqueue(node.Id);
        }

        // Protection contre les diagrammes cycliques.
        if (queue.Count == 0 && diagram.Nodes.Count > 0)
        {
            assignedLevels[diagram.Nodes[0].Id] = 0;
            queue.Enqueue(diagram.Nodes[0].Id);
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var currentLevel = assignedLevels[currentId];

            foreach (var edge in edges.Where(edge =>
                         edge.SourceId == currentId))
            {
                if (!nodesById.ContainsKey(edge.TargetId))
                    continue;

                var nextLevel = currentLevel + 1;

                if (!assignedLevels.TryGetValue(
                        edge.TargetId,
                        out var existingLevel) ||
                    nextLevel > existingLevel)
                {
                    assignedLevels[edge.TargetId] = nextLevel;
                    queue.Enqueue(edge.TargetId);
                }
            }
        }

        // Ajout des nœuds non reliés.
        foreach (var node in diagram.Nodes)
        {
            if (!assignedLevels.ContainsKey(node.Id))
            {
                var nextLevel = assignedLevels.Count == 0
                    ? 0
                    : assignedLevels.Values.Max() + 1;

                assignedLevels[node.Id] = nextLevel;
            }
        }

        var maxLevel = assignedLevels.Values.Max();

        for (var i = 0; i <= maxLevel; i++)
        {
            levels.Add(
                diagram.Nodes
                    .Where(node =>
                        assignedLevels[node.Id] == i)
                    .ToList());
        }

        // Chaque niveau est affiché horizontalement.
        foreach (var level in levels)
        {
            if (level.Count == 0)
                continue;

            column.Item().Row(row =>
            {
                row.Spacing(8);

                foreach (var node in level)
                {
                    row.RelativeItem()
                        .Border(1)
                        .BorderColor(Colors.Grey.Darken1)
                        .Padding(8)
                        .Column(nodeColumn =>
                        {
                            nodeColumn.Item()
                                .AlignCenter()
                                .Text(node.Name)
                                .Bold();

                            nodeColumn.Item()
                                .AlignCenter()
                                .PaddingTop(3)
                                .Text(
                                    $"Type : {GetNodeType(node)}")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);
                        });
                }
            });

            // Affichage des connexions sortantes.
            foreach (var node in level)
            {
                var outgoingEdges = edges
                    .Where(edge => edge.SourceId == node.Id)
                    .ToList();

                foreach (var edge in outgoingEdges)
                {
                    if (!nodesById.TryGetValue(
                            edge.TargetId,
                            out var target))
                    {
                        continue;
                    }

                    column.Item()
                        .PaddingLeft(20)
                        .Text(text =>
                        {
                            text.Span("↓ ").Bold();

                            text.Span(node.Name).Bold();

                            if (!string.IsNullOrWhiteSpace(edge.Label))
                            {
                                text.Span($" ({edge.Label})");
                            }

                            text.Span(" → ");

                            text.Span(target.Name).Bold();
                        });
                }
            }

            column.Item().PaddingBottom(6);
        }
    }

    private static string GetNodeType(
        DocumentationDiagramNodeDto node)
    {
        if (!string.IsNullOrWhiteSpace(node.Type))
            return node.Type;

        if (!string.IsNullOrWhiteSpace(node.NodeType))
            return node.NodeType;

        return "Non défini";
    }
}