namespace PADocGenerator.Api.Services;

public sealed class MicrosoftEntraOptions
{
    public const string SectionName = "MicrosoftEntra";

    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string FrontendRedirectUri { get; set; } = string.Empty;
}