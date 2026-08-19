using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PADocGenerator.Api.Models.Dtos;

namespace PADocGenerator.Api.Services;

public class WordDocumentationRenderer
{
    public byte[] Render(DocumentationDetailDto documentation)
    {
        using var stream = new MemoryStream();

        using (var wordDocument = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   true))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();

            var body = mainPart.Document.AppendChild(new Body());

            // =========================
            // En-tête
            // =========================
            body.AppendChild(CreateHeading(documentation.Title, 1));

            body.AppendChild(CreateParagraph(
                $"Flux source : {documentation.FlowName}    |    " +
                $"Statut : {documentation.Status}    |    " +
                $"Version : v{documentation.CurrentVersionNumber}",
                italic: true));

            body.AppendChild(CreateParagraph(
                $"Mis à jour le {documentation.UpdatedAtUtc:dd/MM/yyyy HH:mm}",
                italic: true,
                small: true));

            // =========================
            // Résumé fonctionnel
            // =========================
            body.AppendChild(CreateHeading(
                "Résumé fonctionnel",
                2));

            body.AppendChild(CreateParagraph(
                documentation.Content.FunctionalSummary ?? string.Empty));

            // =========================
            // Étapes
            // =========================
            if (documentation.Content.Steps?.Count > 0)
            {
                body.AppendChild(CreateHeading(
                    "Étapes du flux",
                    2));

                foreach (var step in documentation.Content.Steps)
                {
                    body.AppendChild(
                        CreateStepBlock(step));
                }
            }

            // =========================
            // Dépendances
            // =========================
            if (documentation.Content.Dependencies?.Count > 0)
            {
                body.AppendChild(CreateHeading(
                    "Dépendances entre actions, conditions et variables",
                    2));

                foreach (var dep in documentation.Content.Dependencies)
                {
                    body.AppendChild(CreateParagraph(
                        $"{dep.From} → {dep.To} : {dep.ExplanationText}",
                        bullet: true));
                }
            }

            // =========================
            // Diagramme
            // =========================
            if (documentation.Content.Diagram?.Nodes?.Count > 0)
            {
                body.AppendChild(CreateHeading(
                    "Diagramme du flux",
                    2));

                body.AppendChild(CreateParagraph(
                    "Représentation technique des relations entre les éléments du flux.",
                    italic: true,
                    small: true));

                AppendDiagram(
                    body,
                    documentation.Content.Diagram);
            }

            body.AppendChild(
                new SectionProperties(
                    new PageSize
                    {
                        Width = 11906,
                        Height = 16838
                    }));
        }

        return stream.ToArray();
    }

    // ==================================================
    // DIAGRAMME
    // ==================================================

    private static void AppendDiagram(
        Body body,
        DocumentationDiagramDto diagram)
    {
        var edges = diagram.Edges ??
                    new List<DocumentationDiagramEdgeDto>();

        var nodesById = diagram.Nodes.ToDictionary(
            node => node.Id);

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

        var assignedLevels =
            new Dictionary<string, int>();

        var queue = new Queue<string>();

        foreach (var node in diagram.Nodes.Where(
                     node => incomingCount[node.Id] == 0))
        {
            assignedLevels[node.Id] = 0;
            queue.Enqueue(node.Id);
        }

        if (queue.Count == 0 &&
            diagram.Nodes.Count > 0)
        {
            assignedLevels[diagram.Nodes[0].Id] = 0;
            queue.Enqueue(diagram.Nodes[0].Id);
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var currentLevel = assignedLevels[currentId];

            foreach (var edge in edges.Where(
                         edge => edge.SourceId == currentId))
            {
                if (!nodesById.ContainsKey(edge.TargetId))
                    continue;

                var nextLevel = currentLevel + 1;

                if (!assignedLevels.TryGetValue(
                        edge.TargetId,
                        out var existingLevel) ||
                    nextLevel > existingLevel)
                {
                    assignedLevels[edge.TargetId] =
                        nextLevel;

                    queue.Enqueue(edge.TargetId);
                }
            }
        }

        foreach (var node in diagram.Nodes)
        {
            if (!assignedLevels.ContainsKey(node.Id))
            {
                assignedLevels[node.Id] =
                    assignedLevels.Count == 0
                        ? 0
                        : assignedLevels.Values.Max() + 1;
            }
        }

        var maxLevel = assignedLevels.Values.Max();

        for (var level = 0;
             level <= maxLevel;
             level++)
        {
            var levelNodes = diagram.Nodes
                .Where(node =>
                    assignedLevels[node.Id] == level)
                .ToList();

            if (levelNodes.Count == 0)
                continue;

            // Les nœuds du même niveau sont placés
            // dans une même ligne.
            body.AppendChild(
                CreateDiagramNodesTable(levelNodes));

            foreach (var node in levelNodes)
            {
                var outgoingEdges = edges
                    .Where(edge =>
                        edge.SourceId == node.Id)
                    .ToList();

                foreach (var edge in outgoingEdges)
                {
                    if (!nodesById.TryGetValue(
                            edge.TargetId,
                            out var target))
                    {
                        continue;
                    }

                    var label =
                        string.IsNullOrWhiteSpace(edge.Label)
                            ? string.Empty
                            : $" [{edge.Label}]";

                    body.AppendChild(CreateParagraph(
                        $"↓ {node.Name}{label} → {target.Name}",
                        bullet: false));
                }
            }

            body.AppendChild(CreateParagraph(""));
        }
    }

    private static Table CreateDiagramNodesTable(
        List<DocumentationDiagramNodeDto> nodes)
    {
        var table = new Table();

        table.AppendChild(
            new TableProperties(
                new TableBorders(
                    new TopBorder
                    {
                        Val = BorderValues.Single,
                        Size = 8
                    },
                    new BottomBorder
                    {
                        Val = BorderValues.Single,
                        Size = 8
                    },
                    new LeftBorder
                    {
                        Val = BorderValues.Single,
                        Size = 8
                    },
                    new RightBorder
                    {
                        Val = BorderValues.Single,
                        Size = 8
                    },
                    new InsideVerticalBorder
                    {
                        Val = BorderValues.Single,
                        Size = 8
                    })));

        var row = new TableRow();

        foreach (var node in nodes)
        {
            var type =
                !string.IsNullOrWhiteSpace(node.Type)
                    ? node.Type
                    : node.NodeType;

            var cell = new TableCell(
                new Paragraph(
                    new ParagraphProperties(
                        new Justification
                        {
                            Val = JustificationValues.Center
                        }),
                    new Run(
                        new RunProperties(new Bold()),
                        new Text(node.Name)),
                    new Break(),
                    new Run(
                        new RunProperties(
                            new FontSize { Val = "18" }),
                        new Text($"Type : {type}"))));

            row.AppendChild(cell);
        }

        table.AppendChild(row);

        return table;
    }

    // ==================================================
    // ÉTAPE
    // ==================================================

    private static Table CreateStepBlock(
        DocumentationStepDto step)
    {
        var table = new Table();

        table.AppendChild(
            new TableProperties(
                new TableBorders(
                    new TopBorder
                    {
                        Val = BorderValues.Single,
                        Size = 6
                    },
                    new BottomBorder
                    {
                        Val = BorderValues.Single,
                        Size = 6
                    },
                    new LeftBorder
                    {
                        Val = BorderValues.Single,
                        Size = 6
                    },
                    new RightBorder
                    {
                        Val = BorderValues.Single,
                        Size = 6
                    })));

        var cell = new TableCell();

        cell.AppendChild(
            CreateParagraph(
                $"{step.StepName} — {step.StepType}"));

        cell.AppendChild(
            CreateParagraph(
                $"Description : {step.Description}"));

        if (!string.IsNullOrWhiteSpace(step.Purpose))
        {
            cell.AppendChild(
                CreateParagraph(
                    $"Objectif : {step.Purpose}"));
        }

        if (step.Variables?.Count > 0)
        {
            cell.AppendChild(
                CreateParagraph(
                    "Variables ou données utilisées :"));

            foreach (var variable in step.Variables)
            {
                var value = string.IsNullOrWhiteSpace(variable.Value)
                    ? string.Empty
                    : $" = {variable.Value}";

                var description =
                    string.IsNullOrWhiteSpace(variable.Description)
                        ? string.Empty
                        : $" — {variable.Description}";

                cell.AppendChild(CreateParagraph(
                    $"{variable.Name}{value}{description}",
                    bullet: true));
            }
        }

        if (step.Inputs?.Count > 0)
        {
            cell.AppendChild(
                CreateParagraph("Entrées / paramètres :"));

            foreach (var input in step.Inputs)
            {
                cell.AppendChild(CreateParagraph(
                    $"{input.Key} : {input.Value}",
                    bullet: true));
            }
        }

        table.AppendChild(
            new TableRow(cell));

        return table;
    }

    // ==================================================
    // MÉTHODES UTILITAIRES
    // ==================================================

    private static Paragraph CreateHeading(
        string text,
        int level)
    {
        return new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines
                {
                    Before = "240",
                    After = "120"
                }),
            new Run(
                new RunProperties(
                    new Bold(),
                    new FontSize
                    {
                        Val = level == 1
                            ? "32"
                            : "26"
                    }),
                new Text(text)));
    }

    private static Paragraph CreateParagraph(
        string text,
        bool italic = false,
        bool bullet = false,
        bool small = false)
    {
        var runProperties = new RunProperties();

        if (italic)
            runProperties.Append(new Italic());

        if (small)
            runProperties.Append(
                new FontSize { Val = "18" });

        var displayText =
            bullet ? $"•  {text}" : text;

        var paragraphProperties =
            new ParagraphProperties();

        if (bullet)
        {
            paragraphProperties.Append(
                new Indentation { Left = "360" });
        }

        return new Paragraph(
            paragraphProperties,
            new Run(
                runProperties,
                new Text(displayText)
                {
                    Space =
                        SpaceProcessingModeValues.Preserve
                }));
    }
}