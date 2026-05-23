namespace Migration.Core.Azure.Identity;

/// <summary>
/// Defines the expected secret and sensitive-setting boundary for Azure runtime configuration.
/// This is a policy/configuration contract only; it does not retrieve secrets.
/// </summary>
public sealed class AzureRuntimeSecretBoundaryOptions
{
    public const string SectionName = "AzureRuntime:Secrets";

    /// <summary>
    /// Source of SQL connection material. SQL remains the primary durable operational store.
    /// </summary>
    public AzureRuntimeSecretSource SqlConnectionSource { get; set; } = AzureRuntimeSecretSource.Unspecified;

    /// <summary>
    /// Source of storage connection material, if a connection string is used instead of managed identity.
    /// </summary>
    public AzureRuntimeSecretSource StorageConnectionSource { get; set; } = AzureRuntimeSecretSource.Unspecified;

    /// <summary>
    /// Source of queue/service bus connection material, if a connection string is used instead of managed identity.
    /// </summary>
    public AzureRuntimeSecretSource QueueConnectionSource { get; set; } = AzureRuntimeSecretSource.Unspecified;

    /// <summary>
    /// Source of external connector credentials such as source/target API secrets.
    /// </summary>
    public AzureRuntimeSecretSource ConnectorCredentialSource { get; set; } = AzureRuntimeSecretSource.Unspecified;

    /// <summary>
    /// Optional Key Vault URI for environments that centralize secrets in Key Vault.
    /// </summary>
    public string? KeyVaultUri { get; set; }

    /// <summary>
    /// Whether plain connection strings are allowed in this environment's host settings.
    /// Production should generally keep this false and prefer identity or Key Vault references.
    /// </summary>
    public bool AllowPlainConnectionStrings { get; set; }
}
