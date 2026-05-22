namespace Migration.ControlPlane.Storage;

public sealed record ArtifactStorageRequest(
    string WorkspaceId,
    string ArtifactKind,
    string ArtifactId,
    string FileName,
    string? ContentType = null);

public sealed record ArtifactStorageDescriptor(
    string WorkspaceId,
    string ArtifactKind,
    string ArtifactId,
    string FileName,
    string ObjectKey,
    CloudStorageLocation Location,
    string? ContentType = null,
    long? Length = null,
    string? ETag = null);

public static class ArtifactStorageKinds
{
    public const string Manifest = "manifest";
    public const string Mapping = "mapping";
    public const string Taxonomy = "taxonomy";
    public const string Other = "other";
    public const string Probe = "probe";
}
