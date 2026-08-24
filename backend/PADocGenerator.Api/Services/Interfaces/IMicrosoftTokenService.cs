public interface IMicrosoftTokenService
{
    Task<string> GetPowerPlatformTokenAsync(
        string userId,
        CancellationToken ct);

    Task<string> GetDataverseTokenAsync(
        string userId,
        string environmentUrl,
        CancellationToken ct);
}