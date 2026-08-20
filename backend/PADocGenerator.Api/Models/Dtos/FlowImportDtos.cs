namespace PADocGenerator.Api.Models.Dtos;

public record FlowImportRequestDto(string FileName, string JsonContent);

/// <summary>Environnement Power Platform auquel l'utilisateur connecté a accès.</summary>
public record PowerPlatformEnvironmentDto(
    string Id,
    string DisplayName,
    string? Type,
    string? State,
    string? DataverseUrl,
    string? TenantId);

/// <summary>Résumé d'un flux cloud disponible dans un environnement Power Platform.</summary>
public record PowerPlatformFlowDto(
    string WorkflowId,
    string DisplayName,
    string? State,
    DateTime? ModifiedAtUtc,
    bool IsManaged);

/// <summary>
/// Identifie le flux à importer. Les jetons Microsoft sont volontairement
/// transmis dans les en-têtes HTTP et ne font jamais partie du corps ni de la base.
/// </summary>
public record PowerPlatformFlowImportRequestDto(string EnvironmentId, string WorkflowId);

public record FlowImportResultDto(
    Guid FlowImportId,
    string Name,
    bool IsValid,
    string? ValidationError,
    int ActionsCount,
    DateTime ImportedAtUtc
);
