namespace Migration.ControlPlane.Storage;

/// <summary>
/// Describes a logical cloud storage location without binding the caller to
/// local file-system paths, Azure Blob SDK types, or connection strings.
/// </summary>
public sealed record CloudStorageLocation(
    string Provider,
    string Root,
    string WorkspaceId,
    string RelativePath,
    string Uri);

public static class CloudStorageProviders
{
    public const string LocalFileSystem = "localFileSystem";
    public const string AzureBlob = "azureBlob";
}
