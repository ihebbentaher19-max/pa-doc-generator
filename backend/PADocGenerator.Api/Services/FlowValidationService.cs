using System.Text.Json;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Implémentation du module d'importation : vérifie que le fichier est un JSON
/// valide et qu'il contient au minimum la structure attendue d'un flux Power
/// Automate exporté (propriété "definition" avec "triggers" et "actions", tel
/// que produit par l'export standard de Power Automate / Logic Apps).
/// </summary>
public class FlowValidationService : IFlowValidationService
{
    public FlowValidationResult Validate(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return new FlowValidationResult(false, UserMessages.EmptyFlowFile);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(jsonContent);
        }
        catch (JsonException)
        {
            return new FlowValidationResult(false, UserMessages.InvalidJsonFile);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new FlowValidationResult(false, UserMessages.InvalidFlowFormat);
            }

            // Un export Power Automate peut être encapsulé sous "properties.definition"
            // (export depuis le portail) ou directement sous "definition".
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

            if (definition.ValueKind != JsonValueKind.Object)
            {
                return new FlowValidationResult(false, UserMessages.InvalidFlowFormat);
            }

            var hasActions = definition.TryGetProperty("actions", out _);
            var hasTriggers = definition.TryGetProperty("triggers", out _);

            if (!hasActions && !hasTriggers)
            {
                return new FlowValidationResult(false,
                    UserMessages.InvalidFlowFormat);
            }

            return new FlowValidationResult(true, null);
        }
    }
}
