namespace PADocGenerator.Api.Models.FlowSchema;

/// <summary>
/// Représentation intermédiaire, produite par le Module de lecture et préparation
/// des données (section 6), qui extrait du JSON Power Automate brut les éléments
/// utiles : actions, conditions, variables, connecteurs. C'est cette structure -
/// et non le JSON brut - qui sert à construire le prompt envoyé au modèle d'IA.
/// </summary>
public class ParsedFlow
{
    public string FlowName { get; set; } = string.Empty;
    public string? Trigger { get; set; }

    public List<ParsedAction> Actions { get; set; } = new();
    public List<ParsedCondition> Conditions { get; set; } = new();
    public List<ParsedVariable> Variables { get; set; } = new();
    public List<ParsedConnector> Connectors { get; set; } = new();
}

public class ParsedAction
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ConnectorReference { get; set; }
    /// <summary>Nom de l'étape précédente (utilisé pour reconstruire l'ordre / les dépendances).</summary>
    public string? RunsAfter { get; set; }
    public Dictionary<string, string> Inputs { get; set; } = new();
}

public class ParsedCondition
{
    public string Name { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public List<string> ActionsIfTrue { get; set; } = new();
    public List<string> ActionsIfFalse { get; set; } = new();
}

public class ParsedVariable
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? InitialValue { get; set; }
}

public class ParsedConnector
{
    public string Name { get; set; } = string.Empty;
    public string ConnectorType { get; set; } = string.Empty;
}
