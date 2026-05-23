namespace MigrationBase.Core.Cloud.Azure.Infrastructure.Identity;

public sealed class AzureCredentialBoundary
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public AzureCredentialResolutionMode ResolutionMode { get; init; } = AzureCredentialResolutionMode.ManagedIdentity;

    public string? ManagedIdentityClientIdSettingName { get; init; }

    public string? TenantIdSettingName { get; init; }

    public bool AllowsDeveloperCredential { get; init; }

    public bool AllowsConnectionStringFallback { get; init; }

    public IList<string> RequiredAppSettings { get; } = new List<string>();

    public IList<string> RequiredRoles { get; } = new List<string>();
}
