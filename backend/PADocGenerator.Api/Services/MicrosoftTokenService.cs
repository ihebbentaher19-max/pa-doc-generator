using Microsoft.Extensions.Options;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Services;

public sealed class MicrosoftTokenService : IMicrosoftTokenService
{
    private readonly MicrosoftEntraOptions _options;

    public MicrosoftTokenService(
        IOptions<MicrosoftEntraOptions> options)
    {
        _options = options.Value;
    }

    public Task<string> GetPowerPlatformTokenAsync(
        string userId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        EnsureMicrosoftConfiguration();

        throw new BusinessException(
            "La connexion Microsoft Entra ID n'est pas encore disponible. " +
            "L'application doit d'abord être configurée avec une App Registration Microsoft.");
    }

    public Task<string> GetDataverseTokenAsync(
        string userId,
        string environmentUrl,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        EnsureMicrosoftConfiguration();

        if (string.IsNullOrWhiteSpace(environmentUrl))
        {
            throw new BusinessException(
                "L'URL de l'environnement Dataverse est invalide.");
        }

        throw new BusinessException(
            "La connexion Microsoft Entra ID n'est pas encore disponible. " +
            "L'application doit d'abord être configurée avec une App Registration Microsoft.");
    }

    private void EnsureMicrosoftConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.TenantId) ||
            string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new BusinessException(
                "Microsoft Entra ID n'est pas encore configuré. " +
                "Les valeurs TenantId et ClientId sont requises.");
        }
    }
}