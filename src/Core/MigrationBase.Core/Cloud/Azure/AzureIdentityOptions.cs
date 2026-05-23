namespace MigrationBase.Core.Cloud.Azure;

public sealed class AzureIdentityOptions
{
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Optional user-assigned managed identity client id. Leave empty for system-assigned identity.
    /// </summary>
    public string ManagedIdentityClientId { get; set; } = string.Empty;

    public bool AllowDeveloperCredentialFallback { get; set; }
}
