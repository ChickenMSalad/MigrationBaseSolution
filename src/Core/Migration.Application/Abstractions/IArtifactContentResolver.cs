namespace Migration.Application.Abstractions;

public interface IArtifactContentResolver
{
    bool IsArtifactReference(string? value);

    Task<ResolvedArtifactContent> OpenReadAsync(
        string artifactReferenceOrId,
        CancellationToken cancellationToken = default);
}

public sealed class ResolvedArtifactContent : IAsyncDisposable, IDisposable
{
    public ResolvedArtifactContent(
        string artifactId,
        string fileName,
        string contentType,
        Stream content)
    {
        ArtifactId = string.IsNullOrWhiteSpace(artifactId)
            ? throw new ArgumentException("Artifact id is required.", nameof(artifactId))
            : artifactId;
        FileName = string.IsNullOrWhiteSpace(fileName)
            ? artifactId
            : fileName;
        ContentType = string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public string ArtifactId { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public Stream Content { get; }

    public void Dispose() => Content.Dispose();

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync().ConfigureAwait(false);
    }
}
