namespace PADocGenerator.Tests;

/// <summary>
/// Flux Power Automate d'exemple partagés entre les fichiers de test, calqués
/// sur le format réel d'export (properties.definition.actions/triggers).
/// </summary>
internal static class SampleFlows
{
    public const string ApprovalFlowJson = """
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
}
