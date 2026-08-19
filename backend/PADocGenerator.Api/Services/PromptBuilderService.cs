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
            (Règles obligatoires :
            - Utilise uniquement les informations techniques fournies dans les données du flux.
            - N'invente aucune étape, variable, dépendance, connecteur ou valeur.
            - Pour chaque étape, renseigne "connector" uniquement avec le connecteur indiqué dans les données du nœud correspondant.
            - Si aucun connecteur n'est fourni pour un nœud, retourne "connector": null.
            - Ne déduis pas un connecteur uniquement à partir du nom de l'étape.
            - Documente chaque étape à partir de ses inputs, expressions et variables réellement détectées.
            - Explique les expressions uniquement lorsqu'elles sont présentes.
            - Si une étape n'utilise aucune variable ou donnée explicitement détectée, retourne une liste "variables": [].
            - Ne crée pas de variables à partir du nom des paramètres ou d'une interprétation du comportement du flux.
            - Pour chaque variable détectée, utilise son nom réel et associe sa valeur uniquement si elle est disponible dans les données fournies.
            - Une variable ne doit être associée à une étape que si elle est réellement détectée dans ses données.
            - Pour les relations techniques possédant un label, utilise ce label comme "relationshipType" sans le modifier.
            - Les labels tels que "Oui", "Non", "True", "False", "If yes" ou "If no" doivent être conservés afin de représenter correctement les branches des conditions.
            - Si une relation n'a aucun label, utilise "Exécution" comme "relationshipType".
            - Conserve le type technique réellement fourni pour chaque étape.
            - Distingue clairement les déclencheurs, actions, conditions, boucles, variables et autres contrôles.
            - Ne transforme pas un déclencheur en action et ne transforme pas une condition en action.
            - Pour chaque étape, la "description" doit expliquer clairement ce que l'étape réalise à partir des informations réellement fournies.
            - Le champ "purpose" doit expliquer le rôle de l'étape dans le déroulement global du flux, sans inventer de logique métier absente des données.
            - Ne te contente pas de répéter le nom ou le type technique de l'étape dans la description.
            - Lorsque les inputs ou expressions permettent de comprendre l'action réalisée, utilise-les pour produire une explication plus précise et compréhensible.
            - Si les informations disponibles sont insuffisantes pour déterminer précisément le comportement d'une étape, reste factuel et décris uniquement ce qui est techniquement identifiable.
            - Rédige la documentation en français professionnel, clair et compréhensible par un utilisateur technique ou fonctionnel connaissant Power Automate.
            - Les noms des étapes doivent rester fidèles aux données du flux, mais les explications doivent utiliser un vocabulaire lisible plutôt qu'une simple répétition des identifiants techniques.
            - Distingue clairement ce que fait une étape ("description") de son rôle dans le processus ("purpose").
            - Le diagramme et les relations techniques ne doivent pas être inventés par l'IA : les dépendances fournies doivent être respectées.)
            {
                "functionalSummary": "résumé fonctionnel professionnel et concis en 3 à 6 phrases décrivant le déclenchement du flux, son objectif principal, les principales décisions et actions réalisées, sans inventer d'informations",
                "steps": [
                    {
                        "stepId": "identifiant technique fourni dans les données",
                        "stepName": "nom lisible de l'étape",
                        "stepType": "type technique de l'étape",
                        "connector": "connecteur réellement détecté ou null",
                        "description": "ce que fait concrètement cette étape",
                        "purpose": "pourquoi cette étape est nécessaire dans le flux",
                        "variables": [
                            {
                                "name": "nom de la variable réellement détectée",
                                "value": "valeur ou expression associée si disponible",
                                "description": "rôle réel de cette variable dans l'étape"
                            }
                        ],
                        "inputs": {
                            "nomParametre": "valeur ou expression réellement fournie"
                        }
                    }
                ],
                "dependencies": [
                    {
                        "from": "identifiant source réellement fourni",
                        "to": "identifiant cible réellement fourni",
                        "explanationText": "explication claire de la relation",
                        "relationshipType": "Après exécution, Oui, Non ou Relation technique"
                    }
                ]
            }
            """;
    }

    public string BuildUserPrompt(ParsedFlow flow)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Nom du flux : {flow.FlowName}");
        sb.AppendLine($"Déclencheur : {flow.Trigger ?? "non identifié"}");
        sb.AppendLine();
        sb.AppendLine("Nœuds techniques du flux :");
        foreach (var node in flow.Nodes)
        {
            sb.AppendLine(
                $"- ID: {node.Id}");
            sb.AppendLine(
                $"  Nom: {node.Name}");
            sb.AppendLine(
                $"  Catégorie: {node.NodeType}");
            sb.AppendLine(
                $"  Type: {node.Type}");

            if (!string.IsNullOrWhiteSpace(node.ConnectorReference))
            {
                sb.AppendLine(
                    $"  Connecteur: {node.ConnectorReference}");
            }

            if (node.Inputs.Count > 0)
            {
                sb.AppendLine("  Inputs :");

                foreach (var input in node.Inputs)
                {
                    sb.AppendLine(
                        $"    - {input.Key}: {input.Value}");
                }
            }

            if (node.UsedVariables.Count > 0)
            {
                sb.AppendLine(
                    $"  Variables utilisées: {string.Join(", ", node.UsedVariables)}");
            }

            if (node.Expressions.Count > 0)
            {
                sb.AppendLine("  Expressions :");

                foreach (var expression in node.Expressions)
                {
                    sb.AppendLine(
                        $"    - {expression}");
                }
            }
        } 
        sb.AppendLine();
        sb.AppendLine("Relations techniques :");

        foreach (var edge in flow.Edges)
        {
            sb.AppendLine(
                $"- {edge.SourceId} -> {edge.TargetId}" +
                (string.IsNullOrWhiteSpace(edge.Label)
                    ? string.Empty
                    : $" [{edge.Label}]"));
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
