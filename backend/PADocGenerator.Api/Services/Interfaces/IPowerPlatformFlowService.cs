using PADocGenerator.Api.Models.Dtos;

namespace PADocGenerator.Api.Services.Interfaces;

/// <summary>
/// Accès délégué en lecture aux API Power Platform et Dataverse. Les jetons
/// représentent l'utilisateur courant et ne sont jamais conservés par l'API.
/// </summary>
public interface IPowerPlatformFlowService
{
    Task<IReadOnlyList<PowerPlatformEnvironmentDto>> GetEnvironmentsAsync(
        string powerPlatformAccessToken,
        CancellationToken ct);

    Task<IReadOnlyList<PowerPlatformFlowDto>> GetFlowsAsync(
        string powerPlatformAccessToken,
        string environmentId,
        CancellationToken ct);

    Task<PowerPlatformFlowDefinitionDto> GetFlowDefinitionAsync(
        string powerPlatformAccessToken,
        string dataverseAccessToken,
        string environmentId,
        string workflowId,
        CancellationToken ct);
}

public record PowerPlatformFlowDefinitionDto(
    string DisplayName,
    string DefinitionJson,
    string EnvironmentId,
    string? TenantId);
