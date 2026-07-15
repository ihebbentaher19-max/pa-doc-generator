using System.Text;
using PADocGenerator.Api.Models.FlowSchema;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Construit dynamiquement le prompt envoyé au modèle d'IA à partir des données
/// préparées par le <see cref="FlowParserService"/> (cf. section 6, module de
/// génération : "Construit dynamiquement le prompt destiné au modèle d'IA à
/// partir des données préparées."). Isolé dans son propre service afin de
/// pouvoir faire évoluer le prompt indépendamment du client Azure OpenAI.
/// </summary>
public class PromptBuilderService
{
    public string BuildSystemPrompt()
    {
        return """
            Tu es un assistant technique spécialisé dans la documentation de flux
            Microsoft Power Automate. À partir d'une description structurée d'un flux
            (déclencheur, actions, conditions, variables, connecteurs), tu dois produire
            une documentation fonctionnelle claire, destinée à des collègues qui n'ont
            pas créé le flux. Réponds UNIQUEMENT avec un objet JSON valide respectant
            exactement le schéma suivant, sans texte hors du JSON :

            {
              "functionalSummary": "résumé fonctionnel du flux en 3 à 6 phrases, en langage naturel",
              "steps": [
                { "stepName": "nom de l'étape", "description": "rôle de l'étape en langage clair", "isImportant": true|false }
              ],
              "dependencies": [
                { "from": "étape ou variable source", "to": "étape ou variable dépendante", "explanationText": "explication du lien" }
              ],
              "importantSteps": ["nom des étapes jugées critiques pour la compréhension du flux"]
            }
            """;
    }

    public string BuildUserPrompt(ParsedFlow flow)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Nom du flux : {flow.FlowName}");
        sb.AppendLine($"Déclencheur : {flow.Trigger ?? "non identifié"}");

        sb.AppendLine();
        sb.AppendLine("Actions :");
        foreach (var action in flow.Actions)
        {
            sb.AppendLine($"- {action.Name} (type: {action.Type}" +
                (action.ConnectorReference is not null ? $", connecteur: {action.ConnectorReference}" : "") +
                (action.RunsAfter is not null ? $", s'exécute après: {action.RunsAfter}" : "") + ")");
        }

        sb.AppendLine();
        sb.AppendLine("Conditions :");
        foreach (var condition in flow.Conditions)
        {
            sb.AppendLine($"- {condition.Name} : expression = {condition.Expression}");
            sb.AppendLine($"  si vrai -> {string.Join(", ", condition.ActionsIfTrue)}");
            sb.AppendLine($"  si faux -> {string.Join(", ", condition.ActionsIfFalse)}");
        }

        sb.AppendLine();
        sb.AppendLine("Variables :");
        foreach (var variable in flow.Variables)
        {
            sb.AppendLine($"- {variable.Name} (type: {variable.Type}, valeur initiale: {variable.InitialValue ?? "n/a"})");
        }

        sb.AppendLine();
        sb.AppendLine("Connecteurs utilisés :");
        foreach (var connector in flow.Connectors)
        {
            sb.AppendLine($"- {connector.Name} ({connector.ConnectorType})");
        }

        return sb.ToString();
    }
}
