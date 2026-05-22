namespace Migration.ControlPlane.Storage;

public sealed class ArtifactStorageService : IArtifactStorageService
{
    private readonly ICloudStoragePathResolver _pathResolver;
    private readonly ICloudBinaryStorageProvider _binaryStorageProvider;

    public ArtifactStorageService(
        ICloudStoragePathResolver pathResolver,
        ICloudBinaryStorageProvider binaryStorageProvider)
    {
        _pathResolver = pathResolver;
        _binaryStorageProvider = binaryStorageProvider;
    }

    public ArtifactStorageDescriptor Resolve(ArtifactStorageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedWorkspaceId = NormalizeSegment(request.WorkspaceId);
        var normalizedKind = NormalizeSegment(request.ArtifactKind);
        var normalizedArtifactId = NormalizeSegment(request.ArtifactId);
        var safeFileName = NormalizeFileName(request.FileName);

        var artifactRoot = _pathResolver.ResolveArtifactRoot(normalizedWorkspaceId, normalizedKind);
        var relativePath = $"{artifactRoot.RelativePath}/{normalizedArtifactId}/{safeFileName}";
        var uri = $"{artifactRoot.Uri.TrimEnd('/', '\\')}/{normalizedArtifactId}/{safeFileName}";

        var location = artifactRoot with
        {
            RelativePath = relativePath,
            Uri = uri
        };

        return new ArtifactStorageDescriptor(
            WorkspaceId: normalizedWorkspaceId,
            ArtifactKind: normalizedKind,
            ArtifactId: normalizedArtifactId,
            FileName: safeFileName,
            ObjectKey: $"{normalizedKind}/{normalizedArtifactId}/{safeFileName}",
            Location: location,
            ContentType: request.ContentType);
    }

    public async Task<ArtifactStorageDescriptor> WriteAsync(
        ArtifactStorageRequest request,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var descriptor = Resolve(request);

        await _binaryStorageProvider.WriteAsync(
            descriptor.Location,
            content,
            descriptor.ContentType,
            cancellationToken).ConfigureAwait(false);

        return descriptor;
    }

    public Task<bool> ExistsAsync(
        ArtifactStorageRequest request,
        CancellationToken cancellationToken = default)
    {
        var descriptor = Resolve(request);
        return _binaryStorageProvider.ExistsAsync(descriptor.Location, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(
        ArtifactStorageRequest request,
        CancellationToken cancellationToken = default)
    {
        var descriptor = Resolve(request);
        return _binaryStorageProvider.OpenReadAsync(descriptor.Location, cancellationToken);
    }

    public Task DeleteAsync(
        ArtifactStorageRequest request,
        CancellationToken cancellationToken = default)
    {
        var descriptor = Resolve(request);
        return _binaryStorageProvider.DeleteAsync(descriptor.Location, cancellationToken);
    }

    private static string NormalizeSegment(string value)
    {
        var sanitized = new string((value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
            .ToArray());

        while (sanitized.Contains("--", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(sanitized)
            ? "default"
            : sanitized.Trim('-');
    }

    private static string NormalizeFileName(string value)
    {
        var fileName = Path.GetFileName(value ?? string.Empty);

        var sanitized = new string(fileName
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' ? ch : '-')
            .ToArray());

        while (sanitized.Contains("--", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(sanitized)
            ? "artifact.bin"
            : sanitized.Trim('-');
    }
}
