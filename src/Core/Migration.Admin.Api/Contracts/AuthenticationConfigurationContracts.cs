namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Safe authentication configuration contract for future Entra ID/OIDC enforcement.
/// This does not expose client secrets and does not enable auth by itself.
/// </summary>
public sealed record AuthenticationConfigurationDescriptor(
    string EnvironmentName,
    string AuthMode,
    bool AuthRequired,
    bool IsConfigured,
    string? AuthorityConfigured,
    string? AudienceConfigured,
    string? ClientIdConfigured,
    string? TenantIdConfigured,
    IReadOnlyList<string> RequiredFrontendSettings,
    IReadOnlyList<string> RequiredApiSettings,
    IReadOnlyList<string> RecommendedTokenClaims,
    IReadOnlyList<string> Warnings);

public static class AuthenticationModes
{
    public const string Disabled = "disabled";
    public const string EntraId = "entraId";
    public const string OpenIdConnect = "openIdConnect";
    public const string Unknown = "unknown";
}


