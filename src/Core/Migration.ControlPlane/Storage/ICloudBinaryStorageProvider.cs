namespace Migration.ControlPlane.Storage;

/// <summary>
/// Abstraction for binary/object storage providers.
/// Initial P2 contract only; no provider implementation yet.
/// </summary>
public interface ICloudBinaryStorageProvider
{
    Task<bool> ExistsAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default);

    Task WriteAsync(
        CloudStorageLocation location,
        Stream content,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default);
}
