namespace Migration.ControlPlane.Artifacts;

public interface IArtifactStore
{
    Task<ControlPlaneArtifactRecord> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        ArtifactKind kind,
        string? projectId = null,
        string? description = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ControlPlaneArtifactRecord>> ListAsync(
        ArtifactKind? kind = null,
        string? projectId = null,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneArtifactRecord?> GetAsync(string artifactId, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string artifactId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string artifactId, CancellationToken cancellationToken = default);

    Task<ManifestPreview> PreviewManifestAsync(string artifactId, int take = 10, CancellationToken cancellationToken = default);
}
