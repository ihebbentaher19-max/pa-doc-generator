using System.Text.Json;
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
                parsedFlow.Trigger = trigger.Name;
                break; // un flux Power Automate n'a en général qu'un déclencheur
            }
        }

        if (definition.TryGetProperty("actions", out var actions))
        {
            ParseActionsRecursively(actions, parsedFlow, parentActionName: null);
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

    private static void ParseActionsRecursively(JsonElement actions, ParsedFlow flow, string? parentActionName)
    {
        foreach (var actionProperty in actions.EnumerateObject())
        {
            var actionName = actionProperty.Name;
            var actionBody = actionProperty.Value;

            var actionType = actionBody.TryGetProperty("type", out var typeEl)
                ? typeEl.GetString() ?? "Unknown"
                : "Unknown";

            string? runsAfter = parentActionName;
            if (actionBody.TryGetProperty("runAfter", out var runAfterEl))
            {
                foreach (var dep in runAfterEl.EnumerateObject())
                {
                    runsAfter = dep.Name; // première dépendance déclarée
                    break;
                }
            }

            // Les branches de type "If" / "Switch" sont traitées comme des conditions.
            if (string.Equals(actionType, "If", StringComparison.OrdinalIgnoreCase))
            {
                var condition = new ParsedCondition
                {
                    Name = actionName,
                    Expression = actionBody.TryGetProperty("expression", out var expr)
                        ? expr.ToString()
                        : string.Empty
                };

                if (actionBody.TryGetProperty("actions", out var trueActions))
                {
                    condition.ActionsIfTrue = trueActions.EnumerateObject().Select(a => a.Name).ToList();
                    ParseActionsRecursively(trueActions, flow, actionName);
                }

                if (actionBody.TryGetProperty("else", out var elseBranch) &&
                    elseBranch.TryGetProperty("actions", out var falseActions))
                {
                    condition.ActionsIfFalse = falseActions.EnumerateObject().Select(a => a.Name).ToList();
                    ParseActionsRecursively(falseActions, flow, actionName);
                }

                flow.Conditions.Add(condition);
                continue;
            }

            // Les variables (InitializeVariable / SetVariable) sont extraites séparément.
            // Schéma réel Power Automate : "inputs": { "variables": [ { "name", "type", "value" } ] }
            // pour InitializeVariable (potentiellement plusieurs variables dans le même tableau),
            // et "inputs": { "name", "value" } (à plat) pour SetVariable. On gère les deux formes.
            if (actionType.Contains("Variable", StringComparison.OrdinalIgnoreCase) &&
                actionBody.TryGetProperty("inputs", out var varInputs))
            {
                if (varInputs.TryGetProperty("variables", out var variablesArray) &&
                    variablesArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var variableEl in variablesArray.EnumerateArray())
                    {
                        var name = variableEl.TryGetProperty("name", out var n) ? n.GetString() : actionName;
                        var type = variableEl.TryGetProperty("type", out var t) ? t.GetString() : "unknown";
                        var value = variableEl.TryGetProperty("value", out var v) ? v.ToString() : null;

                        flow.Variables.Add(new ParsedVariable
                        {
                            Name = name ?? actionName,
                            Type = type ?? "unknown",
                            InitialValue = value
                        });
                    }
                }
                else
                {
                    // Forme à plat (ex. SetVariable) : "inputs": { "name": "...", "value": "..." }
                    var name = varInputs.TryGetProperty("name", out var n) ? n.GetString() : actionName;
                    var value = varInputs.TryGetProperty("value", out var v) ? v.ToString() : null;

                    flow.Variables.Add(new ParsedVariable
                    {
                        Name = name ?? actionName,
                        Type = "unknown",
                        InitialValue = value
                    });
                }
                continue;
            }

            var parsedAction = new ParsedAction
            {
                Name = actionName,
                Type = actionType,
                RunsAfter = runsAfter
            };

            // Le connecteur est en général identifiable via inputs.host.connectionName
            // ou via le préfixe du "type" (ApiConnection, etc.).
            if (actionBody.TryGetProperty("inputs", out var inputs))
            {
                if (inputs.TryGetProperty("host", out var host) &&
                    host.TryGetProperty("connectionName", out var connName))
                {
                    parsedAction.ConnectorReference = connName.GetString();

                    var connectorName = connName.GetString() ?? "connecteur inconnu";
                    if (!flow.Connectors.Any(c => c.Name == connectorName))
                    {
                        flow.Connectors.Add(new ParsedConnector
                        {
                            Name = connectorName,
                            ConnectorType = actionType
                        });
                    }
                }

                foreach (var inputProperty in inputs.EnumerateObject().Take(10))
                {
                    parsedAction.Inputs[inputProperty.Name] = inputProperty.Value.ToString();
                }
            }

            flow.Actions.Add(parsedAction);
        }
    }
}
