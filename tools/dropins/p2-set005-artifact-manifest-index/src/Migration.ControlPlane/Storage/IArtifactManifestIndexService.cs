namespace Migration.ControlPlane.Storage;

public interface IArtifactManifestIndexService
{
    ArtifactStorageDescriptor ResolveIndex(string workspaceId);

    Task<ArtifactManifestIndex> ReadAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    Task<ArtifactManifestIndex> AddAsync(
        ArtifactStorageDescriptor artifact,
        CancellationToken cancellationToken = default);
}
