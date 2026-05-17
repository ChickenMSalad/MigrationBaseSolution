namespace Migration.ControlPlane.Storage;

/// <summary>
/// Safe placeholder provider used before Azure Blob implementation arrives.
/// Throws intentionally if IO is attempted.
/// </summary>
public sealed class NullCloudBinaryStorageProvider : ICloudBinaryStorageProvider
{
    public Task<bool> ExistsAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default)
        => throw NotConfigured();

    public Task<Stream> OpenReadAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default)
        => throw NotConfigured();

    public Task WriteAsync(
        CloudStorageLocation location,
        Stream content,
        string? contentType = null,
        CancellationToken cancellationToken = default)
        => throw NotConfigured();

    public Task DeleteAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default)
        => throw NotConfigured();

    private static InvalidOperationException NotConfigured() =>
        new("""
Cloud binary storage provider is not configured yet.

P2 Set 002 only adds provider contracts.
Future P2 sets will add Azure Blob-backed implementations.
""");
}
