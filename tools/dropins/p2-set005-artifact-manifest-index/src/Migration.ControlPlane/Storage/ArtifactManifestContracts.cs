namespace Migration.ControlPlane.Storage;

public sealed record ArtifactManifestEntry(
    string ArtifactId,
    string ArtifactKind,
    string FileName,
    string ObjectKey,
    string Uri,
    string ContentType,
    DateTimeOffset CreatedUtc);

public sealed record ArtifactManifestIndex(
    string WorkspaceId,
    string SchemaVersion,
    DateTimeOffset UpdatedUtc,
    IReadOnlyList<ArtifactManifestEntry> Artifacts);
