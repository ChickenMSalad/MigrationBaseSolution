namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Safe cloud-facing artifact storage plan. This describes intended artifact
/// storage shape without exposing connection strings or changing upload/download behavior.
/// </summary>
public sealed record ArtifactStoragePlanDescriptor(
    string EnvironmentName,
    string WorkspaceId,
    string ArtifactMode,
    string ProviderKind,
    string ArtifactRoot,
    string ManifestRoot,
    string MappingRoot,
    string TaxonomyRoot,
    string OtherRoot,
    string? BlobContainerName,
    string? BlobAccountName,
    bool UsesLocalFileSystem,
    bool UsesAzureBlob,
    bool RequiresManagedIdentity,
    IReadOnlyList<string> SupportedArtifactKinds,
    IReadOnlyList<string> Warnings);

public static class ArtifactStorageProviderKinds
{
    public const string LocalFileSystem = "localFileSystem";
    public const string AzureBlobConnectionString = "azureBlobConnectionString";
    public const string AzureBlobManagedIdentity = "azureBlobManagedIdentity";
    public const string Unknown = "unknown";
}

public static class ArtifactStorageKinds
{
    public const string Manifest = "manifest";
    public const string Mapping = "mapping";
    public const string Taxonomy = "taxonomy";
    public const string Other = "other";
}
