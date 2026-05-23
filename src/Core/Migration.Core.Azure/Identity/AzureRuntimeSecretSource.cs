namespace Migration.Core.Azure.Identity;

/// <summary>
/// Describes where a runtime secret or sensitive setting is expected to come from.
/// </summary>
public enum AzureRuntimeSecretSource
{
    /// <summary>
    /// No source has been selected.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The value is expected to come from Azure Key Vault.
    /// </summary>
    KeyVault = 1,

    /// <summary>
    /// The value is expected to be injected through host environment variables or platform app settings.
    /// </summary>
    HostEnvironment = 2,

    /// <summary>
    /// The value is expected to come from local developer configuration only.
    /// </summary>
    LocalDevelopment = 3
}
