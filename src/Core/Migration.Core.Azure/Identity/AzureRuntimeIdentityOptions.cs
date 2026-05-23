namespace Migration.Core.Azure.Identity;

/// <summary>
/// Shared cloud identity settings for Azure-hosted MigrationBaseSolution processes.
/// This class intentionally avoids Azure SDK dependencies so it can be used by hosts,
/// workers, validators, and deployment tooling without forcing a runtime package choice.
/// </summary>
public sealed class AzureRuntimeIdentityOptions
{
    public const string SectionName = "AzureRuntime:Identity";

    /// <summary>
    /// The identity mode expected for the current runtime environment.
    /// </summary>
    public AzureRuntimeIdentityMode Mode { get; set; } = AzureRuntimeIdentityMode.Unspecified;

    /// <summary>
    /// Client ID for user-assigned managed identity. Required only when Mode is UserAssignedManagedIdentity.
    /// </summary>
    public string? UserAssignedClientId { get; set; }

    /// <summary>
    /// Azure tenant ID for environments that require explicit tenant binding.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Whether local developer credentials are allowed for this environment.
    /// Production configuration should leave this false.
    /// </summary>
    public bool AllowDeveloperCredential { get; set; }

    /// <summary>
    /// Optional operator-facing note explaining the identity expectation for this environment.
    /// </summary>
    public string? OperationalNote { get; set; }

    public bool RequiresUserAssignedClientId => Mode == AzureRuntimeIdentityMode.UserAssignedManagedIdentity;
}
