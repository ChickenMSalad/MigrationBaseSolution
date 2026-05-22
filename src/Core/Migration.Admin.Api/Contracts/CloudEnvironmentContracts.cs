namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Cloud-facing environment descriptor for the Admin API.
/// This intentionally exposes only safe, non-secret runtime shape information.
/// </summary>
public sealed record CloudEnvironmentDescriptor(
    string EnvironmentName,
    string HostKind,
    string StorageMode,
    string QueueProvider,
    string? QueueName,
    string CredentialMode,
    string ArtifactMode,
    string? ControlPlaneStorageRoot,
    bool IsLocal,
    bool IsCloudReady,
    IReadOnlyList<string> Warnings);

public static class CloudHostKinds
{
    public const string LocalDevelopment = "localDevelopment";
    public const string AppService = "appService";
    public const string ContainerApp = "containerApp";
    public const string Functions = "functions";
    public const string Unknown = "unknown";
}

public static class CloudCredentialModes
{
    public const string Local = "local";
    public const string UserSecrets = "userSecrets";
    public const string KeyVault = "keyVault";
    public const string ManagedIdentity = "managedIdentity";
    public const string Unknown = "unknown";
}

public static class CloudArtifactModes
{
    public const string LocalFileSystem = "localFileSystem";
    public const string AzureBlob = "azureBlob";
    public const string Unknown = "unknown";
}
