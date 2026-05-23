namespace Migration.Core.Azure.Identity;

/// <summary>
/// Describes how cloud-hosted processes should authenticate to Azure resources.
/// </summary>
public enum AzureRuntimeIdentityMode
{
    /// <summary>
    /// No identity mode has been selected. This should only be used for local placeholder configuration.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Use the hosting resource's system-assigned managed identity.
    /// </summary>
    SystemAssignedManagedIdentity = 1,

    /// <summary>
    /// Use an explicitly configured user-assigned managed identity.
    /// </summary>
    UserAssignedManagedIdentity = 2,

    /// <summary>
    /// Use local developer credentials. This is intended for development only.
    /// </summary>
    DeveloperCredential = 3
}
