namespace Migration.ControlPlane.Storage;

/// <summary>
/// Local file-system implementation of the binary storage provider contract.
/// This is useful for development and for proving the storage abstraction before
/// Azure Blob is introduced.
/// </summary>
public sealed class LocalFileSystemCloudBinaryStorageProvider : ICloudBinaryStorageProvider
{
    public Task<bool> ExistsAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        var path = ToLocalPath(location);
        return Task.FromResult(File.Exists(path));
    }

    public Task<Stream> OpenReadAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        var path = ToLocalPath(location);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Cloud binary storage object was not found.", path);
        }

        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public async Task WriteAsync(
        CloudStorageLocation location,
        Stream content,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(content);

        var path = ToLocalPath(location);
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        var path = ToLocalPath(location);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private static string ToLocalPath(CloudStorageLocation location)
    {
        if (!string.Equals(location.Provider, CloudStorageProviders.LocalFileSystem, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Local file-system provider cannot handle storage provider '{location.Provider}'.");
        }

        var root = location.Root.Trim().TrimEnd('/', '\\');
        var relative = location.RelativePath.Trim().TrimStart('/', '\\');

        var parts = relative
            .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

        return Path.Combine(new[] { root }.Concat(parts).ToArray());
    }
}
