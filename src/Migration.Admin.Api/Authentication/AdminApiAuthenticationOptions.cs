namespace Migration.Admin.Api.Authentication;

/// <summary>
/// Authentication options for the Admin API. This is intentionally lightweight
/// and does not require any external authentication packages yet.
/// </summary>
public sealed class AdminApiAuthenticationOptions
{
    public const string SectionName = "Auth";

    public string Mode { get; init; } = "disabled";

    public bool Required { get; init; }

    public string? Authority { get; init; }

    public string? Audience { get; init; }

    public string? TenantId { get; init; }

    public bool IsEnabled =>
        Required ||
        string.Equals(Mode, "entraId", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Mode, "openIdConnect", StringComparison.OrdinalIgnoreCase);
}
