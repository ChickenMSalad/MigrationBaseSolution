namespace Migration.ControlPlane.Storage;

public interface IArtifactStorageService
{
    ArtifactStorageDescriptor Resolve(ArtifactStorageRequest request);

    Task<ArtifactStorageDescriptor> WriteAsync(
        ArtifactStorageRequest request,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        ArtifactStorageRequest request,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        ArtifactStorageRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ArtifactStorageRequest request,
        CancellationToken cancellationToken = default);
}
