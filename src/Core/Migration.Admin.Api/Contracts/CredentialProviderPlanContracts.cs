namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Safe cloud-facing credential provider plan. This describes how credential
/// material should be resolved in the current environment without exposing
/// secrets or changing current credential behavior.
/// </summary>
public sealed record CredentialProviderPlanDescriptor(
    string EnvironmentName,
    string CredentialMode,
    string WorkspaceId,
    string? TenantId,
    string ProviderKind,
    bool UsesLocalSecrets,
    bool UsesUserSecrets,
    bool UsesKeyVault,
    bool UsesManagedIdentity,
    string? KeyVaultName,
    string? KeyVaultUri,
    string SecretNamePrefix,
    IReadOnlyList<string> SupportedSecretKinds,
    IReadOnlyList<string> Warnings);

public static class CredentialProviderKinds
{
    public const string LocalDevelopment = "localDevelopment";
    public const string UserSecrets = "userSecrets";
    public const string KeyVault = "keyVault";
    public const string ManagedIdentityKeyVault = "managedIdentityKeyVault";
    public const string Unknown = "unknown";
}
