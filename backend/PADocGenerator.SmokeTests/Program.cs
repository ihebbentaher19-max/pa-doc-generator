using System.Text.Json;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services;

int passed = 0;
int failed = 0;

void Check(string testName, bool condition, string? detail = null)
{
    if (condition)
    {
        passed++;
        Console.WriteLine($"  [OK]   {testName}");
    }
    else
    {
        failed++;
        Console.WriteLine($"  [FAIL] {testName}" + (detail is not null ? $" -- {detail}" : ""));
    }
}

var validationService = new FlowValidationService();
var parserService = new FlowParserService();
var formattingService = new DocumentFormattingService();

// ---------------------------------------------------------------------
// 1. FlowValidationService
// ---------------------------------------------------------------------
Console.WriteLine("== FlowValidationService ==");

Check("Fichier vide -> invalide", !validationService.Validate("").IsValid);
Check("Fichier chaine blanche -> invalide", !validationService.Validate("   ").IsValid);
Check("JSON malforme -> invalide", !validationService.Validate("{ this is not json ").IsValid);

var jsonSansActionsNiTriggers = """{ "name": "Flux vide", "properties": { "definition": { "foo": "bar" } } }""";
var resultSansActions = validationService.Validate(jsonSansActionsNiTriggers);
Check("JSON valide mais sans actions/triggers -> invalide", !resultSansActions.IsValid, resultSansActions.Error);

const string validFlowJson = """
{
  "name": "Approval Flow",
  "properties": {
    "definition": {
      "$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
      "triggers": {
        "manual": { "type": "Request", "kind": "Http" }
      },
      "actions": {
        "Send_an_email": {
          "type": "ApiConnection",
          "inputs": {
            "host": { "connectionName": "shared_office365", "operationId": "SendEmailV2" },
            "parameters": { "emailMessage/To": "manager@contoso.com", "emailMessage/Subject": "Approval needed" }
          },
          "runAfter": {}
        },
        "Initialize_counter": {
          "type": "InitializeVariable",
          "inputs": {
            "variables": [
              { "name": "counter", "type": "integer", "value": 0 }
            ]
          },
          "runAfter": { "Send_an_email": ["Succeeded"] }
        },
        "Condition": {
          "type": "If",
          "expression": { "equals": ["@triggerBody()?['status']", "Approved"] },
          "runAfter": { "Initialize_counter": ["Succeeded"] },
          "actions": {
            "Approve_request": {
              "type": "ApiConnection",
              "inputs": {
                "host": { "connectionName": "shared_sharepointonline", "operationId": "PatchItem" },
                "parameters": { "item/Status": "Approved" }
              },
              "runAfter": {}
            }
          },
          "else": {
            "actions": {
              "Reject_request": {
                "type": "ApiConnection",
                "inputs": {
                  "host": { "connectionName": "shared_sharepointonline", "operationId": "PatchItem" },
                  "parameters": { "item/Status": "Rejected" }
                },
                "runAfter": {}
              }
            }
          }
        }
      }
    }
  }
}
""";

var resultValid = validationService.Validate(validFlowJson);
Check("Flux Power Automate realiste -> valide", resultValid.IsValid, resultValid.Error);

// ---------------------------------------------------------------------
// 2. FlowParserService
// ---------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("== FlowParserService ==");

var parsed = parserService.Parse(validFlowJson);

Check("Nom du flux extrait", parsed.FlowName == "Approval Flow", $"obtenu: '{parsed.FlowName}'");
Check("Declencheur extrait", parsed.Trigger == "manual", $"obtenu: '{parsed.Trigger}'");
Check("3 actions non-condition/non-variable extraites (Send_an_email, Approve_request, Reject_request)",
    parsed.Actions.Count == 3, $"obtenu: {parsed.Actions.Count} -> [{string.Join(", ", parsed.Actions.Select(a => a.Name))}]");
Check("1 condition extraite", parsed.Conditions.Count == 1, $"obtenu: {parsed.Conditions.Count}");
Check("1 variable extraite", parsed.Variables.Count == 1, $"obtenu: {parsed.Variables.Count}");
Check("Nom de variable correct (counter)",
    parsed.Variables.Count == 1 && parsed.Variables[0].Name == "counter",
    parsed.Variables.Count == 1 ? $"obtenu: '{parsed.Variables[0].Name}'" : "aucune variable");
Check("Type de variable correct (integer)",
    parsed.Variables.Count == 1 && parsed.Variables[0].Type == "integer");
Check("2 connecteurs distincts extraits (office365, sharepointonline)",
    parsed.Connectors.Count == 2, $"obtenu: {parsed.Connectors.Count} -> [{string.Join(", ", parsed.Connectors.Select(c => c.Name))}]");

var sendEmailAction = parsed.Actions.FirstOrDefault(a => a.Name == "Send_an_email");
Check("Send_an_email n'a pas de dependance (premiere action)", sendEmailAction?.RunsAfter is null,
    $"RunsAfter='{sendEmailAction?.RunsAfter}'");

var condition = parsed.Conditions.FirstOrDefault();
Check("Condition a une branche vrai avec Approve_request",
    condition is not null && condition.ActionsIfTrue.Contains("Approve_request"));
Check("Condition a une branche faux avec Reject_request",
    condition is not null && condition.ActionsIfFalse.Contains("Reject_request"));

// Cas limite : flux avec uniquement un trigger, aucune action
const string triggerOnlyFlow = """
{
  "name": "Trigger Only",
  "definition": {
    "triggers": { "recurrence": { "type": "Recurrence" } },
    "actions": {}
  }
}
""";
var parsedTriggerOnly = parserService.Parse(triggerOnlyFlow);
Check("Flux avec trigger seul: 0 action, pas d'exception", parsedTriggerOnly.Actions.Count == 0);
Check("Flux avec trigger seul: nom repli sur 'name' racine quand pas de displayName",
    parsedTriggerOnly.FlowName == "Trigger Only", $"obtenu: '{parsedTriggerOnly.FlowName}'");

// Cas limite : action HTTP brute sans host.connectionName -> ne doit pas planter, pas de connecteur ajoute
const string httpActionFlow = """
{
  "name": "Http Flow",
  "definition": {
    "triggers": { "manual": { "type": "Request" } },
    "actions": {
      "Call_external_api": {
        "type": "Http",
        "inputs": { "method": "GET", "uri": "https://example.com/api" },
        "runAfter": {}
      }
    }
  }
}
""";
var parsedHttp = parserService.Parse(httpActionFlow);
Check("Action HTTP sans host -> 1 action extraite, pas d'exception", parsedHttp.Actions.Count == 1);
Check("Action HTTP sans host -> aucun connecteur ajoute", parsedHttp.Connectors.Count == 0,
    $"obtenu: {parsedHttp.Connectors.Count}");

// Cas limite : flux exporte sans le wrapper "properties" (definition directement a la racine)
const string flatDefinitionFlow = """
{
  "definition": {
    "triggers": { "manual": { "type": "Request" } },
    "actions": {
      "Only_action": { "type": "Compose", "inputs": { "inputs": "hello" }, "runAfter": {} }
    }
  }
}
""";
var parsedFlat = parserService.Parse(flatDefinitionFlow);
Check("Flux sans wrapper 'properties' -> 1 action extraite", parsedFlat.Actions.Count == 1,
    $"obtenu: {parsedFlat.Actions.Count}");
Check("Flux sans wrapper 'properties' -> nom par defaut 'Flux sans nom'",
    parsedFlat.FlowName == "Flux sans nom", $"obtenu: '{parsedFlat.FlowName}'");

// ---------------------------------------------------------------------
// 3. DocumentFormattingService
// ---------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("== DocumentFormattingService ==");

var rawContent = new DocumentationContentDto(
    "  Ce flux gere les demandes d'approbation.  ",
    new List<DocumentationStepDto>
    {
        new("Etape secondaire", "  description B  ", false),
        new("Etape critique", "  description A  ", true),
    },
    new List<DocumentationDependencyDto>
    {
        new("A", "B", "premiere explication"),
        new("A", "B", "explication dupliquee -> doit etre supprimee"),
        new("B", "C", "autre lien"),
    },
    new List<string> { "Etape critique", "Etape critique", "  ", "Autre etape importante" }
);

var formatted = formattingService.Format(rawContent);

Check("Resume nettoye (trim)", formatted.FunctionalSummary == "Ce flux gere les demandes d'approbation.",
    $"obtenu: '{formatted.FunctionalSummary}'");
Check("Etapes importantes affichees en premier",
    formatted.Steps.First().IsImportant, $"premiere etape importante = {formatted.Steps.First().IsImportant}");
Check("Descriptions d'etapes nettoyees (trim)",
    formatted.Steps.All(s => s.Description == s.Description.Trim() && !s.Description.StartsWith(" ")));
Check("Dependances dedupliquees (3 -> 2)",
    formatted.Dependencies.Count == 2, $"obtenu: {formatted.Dependencies.Count}");
Check("Etapes importantes deduplicees et nettoyees (4 entrees -> 2 valides)",
    formatted.ImportantSteps.Count == 2, $"obtenu: {formatted.ImportantSteps.Count} -> [{string.Join(", ", formatted.ImportantSteps)}]");

// ---------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine($"===== RESULTATS : {passed} reussis / {failed} echoues (total {passed + failed}) =====");

if (failed > 0)
{
    Environment.Exit(1);
}
