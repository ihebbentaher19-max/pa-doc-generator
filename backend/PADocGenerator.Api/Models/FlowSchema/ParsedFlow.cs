namespace PADocGenerator.Api.Models.FlowSchema;

public class ParsedFlow
{
    public string FlowName { get; set; } = string.Empty;

    public string? Trigger { get; set; }
    /// <summary>
    /// Identifiant technique du déclencheur utilisé pour construire les relations.
    /// </summary>
    public string? TriggerId { get; set; }

    public List<ParsedFlowNode> Nodes { get; set; } = new();

    public List<ParsedFlowEdge> Edges { get; set; } = new();

    public List<ParsedVariable> Variables { get; set; } = new();

    public List<ParsedConnector> Connectors { get; set; } = new();
}

public class ParsedFlowNode
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string NodeType { get; set; } = string.Empty;

    public string? ConnectorReference { get; set; }

    public Dictionary<string, string> Inputs { get; set; } = new();

    public List<string> UsedVariables { get; set; } = new();

    public List<string> Expressions { get; set; } = new();
}

public class ParsedFlowEdge
{
    public string SourceId { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string? Label { get; set; }
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