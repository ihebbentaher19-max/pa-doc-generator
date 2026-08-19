using System.Text.Json;
using System.Text.RegularExpressions;
using PADocGenerator.Api.Models.FlowSchema;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Implémentation du module de lecture et préparation des données (section 6).
/// Lit automatiquement le JSON du flux Power Automate, extrait les actions,
/// conditions, variables et connecteurs, et prépare un objet <see cref="ParsedFlow"/>
/// exploitable par le module de génération. Ne réalise volontairement aucune
/// analyse technique avancée (cf. section 3 de l'objectif du projet).
/// </summary>
public class FlowParserService : IFlowParserService
{
    /// <summary>
    /// Les clés d'actions/déclencheurs Power Automate ne peuvent pas contenir
    /// d'espaces (contrainte du format JSON exporté) : Power Automate remplace
    /// automatiquement les espaces par des underscores lors de la création du
    /// flux (ex. "Vérifier la priorité" devient "Verifier_la_priorite"). On
    /// reconvertit ces underscores en espaces dès l'extraction, pour que ni le
    /// prompt envoyé à l'IA, ni l'interface, ni les exports PDF/Word n'affichent
    /// de noms techniques illisibles.
    /// </summary>
    private static readonly Regex VariableRegex = new(@"variables\(\s*['""]([^'""]+)['""]\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static string Humanize(string rawName) => rawName.Replace('_', ' ').Trim();

    public ParsedFlow Parse(string jsonContent)
    {
        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        var definition = root;
        if (root.TryGetProperty("properties", out var properties) &&
            properties.TryGetProperty("definition", out var nestedDefinition))
        {
            definition = nestedDefinition;
        }
        else if (root.TryGetProperty("definition", out var directDefinition))
        {
            definition = directDefinition;
        }

        var parsedFlow = new ParsedFlow
        {
            FlowName = TryGetFlowName(root, properties: root.TryGetProperty("properties", out var p) ? p : (JsonElement?)null),
        };

    if (definition.TryGetProperty("triggers", out var triggers))
{
    foreach (var trigger in triggers.EnumerateObject())
    {
        var triggerName = Humanize(trigger.Name);

        parsedFlow.TriggerId = trigger.Name;
        parsedFlow.Trigger = trigger.Name;
        var triggerId = trigger.Name;
        var triggerNode = new ParsedFlowNode
        {
            Id = triggerId,
            Name = triggerName,
            Type = trigger.Value.TryGetProperty("type", out var triggerType)
                ? triggerType.GetString() ?? "Trigger"
                : "Trigger",
            NodeType = "Trigger"
        };

        parsedFlow.Nodes.Add(triggerNode);

        break;
    }
}

        if (definition.TryGetProperty("actions", out var actions))
        {
        ParseActionsRecursively(actions, parsedFlow, parentNodeId: parsedFlow.TriggerId);
        }
        if (definition.TryGetProperty("staticResults", out _))
        {
            // Réservé à une éventuelle extension future (résultats simulés) -
            // non exploité pour la documentation fonctionnelle.
        }

        return parsedFlow;
    }

    private static string TryGetFlowName(JsonElement root, JsonElement? properties)
    {
        if (properties.HasValue && properties.Value.TryGetProperty("displayName", out var displayName))
            return displayName.GetString() ?? "Flux sans nom";

        if (root.TryGetProperty("name", out var name))
            return name.GetString() ?? "Flux sans nom";

        return "Flux sans nom";
    }
    private static void ExtractConnector(
        JsonElement inputs,
        ParsedFlowNode node,
        ParsedFlow flow,
        string actionType)
    {
        if (!inputs.TryGetProperty("host", out var host) ||
            host.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        string? connectorName = null;

        if (host.TryGetProperty("connectionName", out var connectionName))
        {
            connectorName = connectionName.GetString();
        }

        if (string.IsNullOrWhiteSpace(connectorName) &&
            host.TryGetProperty("apiId", out var apiId))
        {
            connectorName = apiId.GetString();
        }

        if (string.IsNullOrWhiteSpace(connectorName))
        {
            return;
        }

        node.ConnectorReference = connectorName;

        if (!flow.Connectors.Any(c =>
            string.Equals(
                c.Name,
                connectorName,
                StringComparison.OrdinalIgnoreCase)))
        {
            flow.Connectors.Add(new ParsedConnector
            {
                Name = connectorName,
                ConnectorType = actionType
            });
        }
    }
    private static void ParseActionsRecursively(
    JsonElement actions,
    ParsedFlow flow,
    string? parentNodeId)
{
    foreach (var actionProperty in actions.EnumerateObject())
    {
        var actionId = actionProperty.Name;
        var actionName = Humanize(actionId);
        var actionBody = actionProperty.Value;

        var actionType = actionBody.TryGetProperty("type", out var typeEl)
            ? typeEl.GetString() ?? "Unknown"
            : "Unknown";

        // Les conditions deviennent elles aussi des nœuds du diagramme.
        if (string.Equals(actionType, "If", StringComparison.OrdinalIgnoreCase))
        {
            var conditionNode = new ParsedFlowNode
            {
                Id = actionId,
                Name = actionName,
                Type = actionType,
                NodeType = "Condition"
            };

            if (actionBody.TryGetProperty("expression", out var expression))
            {
                conditionNode.Expressions.Add(expression.ToString());
                conditionNode.Inputs["condition"] = expression.ToString();
                AddVariablesFromText(
                    expression.ToString(),
                    conditionNode.UsedVariables);
            }

            flow.Nodes.Add(conditionNode);

            AddIncomingEdges(
                actionBody,
                flow,
                actionId,
                parentNodeId);

            if (actionBody.TryGetProperty("actions", out var trueActions))
            {
                var firstTrueActionId =
                    trueActions.EnumerateObject()
                        .Select(a => a.Name)
                        .FirstOrDefault();

                if (firstTrueActionId is not null)
                {
                    flow.Edges.Add(new ParsedFlowEdge
                    {
                        SourceId = actionId,
                        TargetId = firstTrueActionId,
                        Label = "Oui"
                    });
                }

                ParseActionsRecursively(
                    trueActions,
                    flow,
                    parentNodeId: null);
            }

            if (actionBody.TryGetProperty("else", out var elseBranch) &&
                elseBranch.TryGetProperty("actions", out var falseActions))
            {
                var firstFalseActionId =
                    falseActions.EnumerateObject()
                        .Select(a => a.Name)
                        .FirstOrDefault();

                if (firstFalseActionId is not null)
                {
                    flow.Edges.Add(new ParsedFlowEdge
                    {
                        SourceId = actionId,
                        TargetId = firstFalseActionId,
                        Label = "Non"
                    });
                }

                ParseActionsRecursively(
                    falseActions,
                    flow,
                    parentNodeId: null);
            }

            continue;
        }

        // Extraction des variables déclarées.
        if (actionType.Contains(
                "Variable",
                StringComparison.OrdinalIgnoreCase) &&
            actionBody.TryGetProperty("inputs", out var varInputs))
        {
            ExtractDeclaredVariables(varInputs, actionName, flow);
        }

        var node = new ParsedFlowNode
        {
            Id = actionId,
            Name = actionName,
            Type = actionType,
            NodeType = "Action"
        };

        if (actionBody.TryGetProperty("inputs", out var inputs))
        {
            ExtractNodeInputs(inputs, node);
            ExtractConnector(
                inputs,
                node,
                flow,
                actionType);
        }

        flow.Nodes.Add(node);

        AddIncomingEdges(
            actionBody,
            flow,
            actionId,
            parentNodeId);
    }
    }
    private static string? GetConnectorDisplayName(JsonElement host, string? connectionName)
    {
    if (host.TryGetProperty("apiId", out var apiId))
    {
        var apiIdValue = apiId.GetString();

        if (!string.IsNullOrWhiteSpace(apiIdValue))
        {
            var connectorId = apiIdValue
                .Split('/')
                .LastOrDefault();

            if (!string.IsNullOrWhiteSpace(connectorId))
            {
                return connectorId switch
                {
                    "shared_sharepointonline" => "SharePoint",
                    "shared_teams" => "Microsoft Teams",
                    "shared_office365" => "Office 365 Outlook",
                    _ => connectorId.Replace("shared_", "")
                };
            }
        }
    }
    return connectionName;
    }
    private static void AddIncomingEdges(
    JsonElement actionBody,
    ParsedFlow flow,
    string targetId,
    string? parentNodeId)
{
    var hasRunAfter = false;

    if (actionBody.TryGetProperty("runAfter", out var runAfterEl) &&
        runAfterEl.ValueKind == JsonValueKind.Object)
    {
        foreach (var dependency in runAfterEl.EnumerateObject())
        {
            flow.Edges.Add(new ParsedFlowEdge
            {
                SourceId = dependency.Name,
                TargetId = targetId,
                Label = "Après exécution"
            });

            hasRunAfter = true;
        }
    }

    // Si aucune dépendance explicite n'existe, on rattache l'action
    // à son parent logique.
    if (!hasRunAfter && !string.IsNullOrWhiteSpace(parentNodeId))
    {
        flow.Edges.Add(new ParsedFlowEdge
        {
            SourceId = parentNodeId,
            TargetId = targetId,
            Label = null
        });
    }
}

private static void ExtractDeclaredVariables(
    JsonElement varInputs,
    string fallbackActionName,
    ParsedFlow flow)
{
    if (varInputs.TryGetProperty("variables", out var variablesArray) &&
        variablesArray.ValueKind == JsonValueKind.Array)
    {
        foreach (var variableEl in variablesArray.EnumerateArray())
        {
            var name = variableEl.TryGetProperty("name", out var n)
                ? n.GetString()
                : fallbackActionName;

            var type = variableEl.TryGetProperty("type", out var t)
                ? t.GetString()
                : "unknown";

            var value = variableEl.TryGetProperty("value", out var v)
                ? v.ToString()
                : null;

            AddVariableIfMissing(
                flow,
                name ?? fallbackActionName,
                type ?? "unknown",
                value);
        }

        return;
    }

    var flatName = varInputs.TryGetProperty("name", out var nameEl)
        ? nameEl.GetString()
        : fallbackActionName;

    var flatValue = varInputs.TryGetProperty("value", out var valueEl)
        ? valueEl.ToString()
        : null;

    AddVariableIfMissing(
        flow,
        flatName ?? fallbackActionName,
        "unknown",
        flatValue);
}

private static void AddVariableIfMissing(
    ParsedFlow flow,
    string name,
    string type,
    string? initialValue)
{
    if (flow.Variables.Any(v =>
        string.Equals(
            v.Name,
            name,
            StringComparison.OrdinalIgnoreCase)))
    {
        return;
    }

    flow.Variables.Add(new ParsedVariable
    {
        Name = name,
        Type = type,
        InitialValue = initialValue
    });
}

private static void ExtractNodeInputs(
    JsonElement inputs,
    ParsedFlowNode node)
{
    foreach (var inputProperty in inputs.EnumerateObject())
    {
        var value = inputProperty.Value.ToString();

        node.Inputs[inputProperty.Name] = value;

        AddVariablesFromText(value, node.UsedVariables);

        if (LooksLikeExpression(value))
        {
            node.Expressions.Add(value);
        }
    }
}
    private static void AddVariablesFromText(
    string text,
    List<string> variables)
    {
    foreach (Match match in VariableRegex.Matches(text))
    {
        var variableName = match.Groups[1].Value;

        if (!variables.Any(v =>
            string.Equals(
                v,
                variableName,
                StringComparison.OrdinalIgnoreCase)))
        {
            variables.Add(variableName);
        }
    }
    }

    private static bool LooksLikeExpression(string value)
    {
    return value.Contains("@", StringComparison.Ordinal) ||
           value.Contains("variables(", StringComparison.OrdinalIgnoreCase) ||
           value.Contains("outputs(", StringComparison.OrdinalIgnoreCase) ||
           value.Contains("body(", StringComparison.OrdinalIgnoreCase) ||
           value.Contains("trigger", StringComparison.OrdinalIgnoreCase);
    }
}