using System.Text.Json;

namespace Migration.ControlPlane.Storage;

public sealed class ArtifactManifestIndexService : IArtifactManifestIndexService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IArtifactStorageService _artifactStorage;

    public ArtifactManifestIndexService(IArtifactStorageService artifactStorage)
    {
        _artifactStorage = artifactStorage;
    }

    public ArtifactStorageDescriptor ResolveIndex(string workspaceId) =>
        _artifactStorage.Resolve(new ArtifactStorageRequest(
            WorkspaceId: workspaceId,
            ArtifactKind: ArtifactStorageKinds.Other,
            ArtifactId: "_index",
            FileName: "artifact-manifest-index.json",
            ContentType: "application/json"));

    public async Task<ArtifactManifestIndex> ReadAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var request = new ArtifactStorageRequest(
            workspaceId,
            ArtifactStorageKinds.Other,
            "_index",
            "artifact-manifest-index.json",
            "application/json");

        if (!await _artifactStorage.ExistsAsync(request, cancellationToken).ConfigureAwait(false))
        {
            return Empty(workspaceId);
        }

        await using var stream = await _artifactStorage.OpenReadAsync(request, cancellationToken).ConfigureAwait(false);

        var index = await JsonSerializer.DeserializeAsync<ArtifactManifestIndex>(
            stream,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);

        return index ?? Empty(workspaceId);
    }

    public async Task<ArtifactManifestIndex> AddAsync(
        ArtifactStorageDescriptor artifact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var existing = await ReadAsync(artifact.WorkspaceId, cancellationToken).ConfigureAwait(false);

        var nextEntry = new ArtifactManifestEntry(
            ArtifactId: artifact.ArtifactId,
            ArtifactKind: artifact.ArtifactKind,
            FileName: artifact.FileName,
            ObjectKey: artifact.ObjectKey,
            Uri: artifact.Location.Uri,
            ContentType: artifact.ContentType ?? "application/octet-stream",
            CreatedUtc: DateTimeOffset.UtcNow);

        var artifacts = existing.Artifacts
            .Where(item => !string.Equals(item.ObjectKey, nextEntry.ObjectKey, StringComparison.OrdinalIgnoreCase))
            .Append(nextEntry)
            .OrderBy(item => item.ArtifactKind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ArtifactId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var updated = new ArtifactManifestIndex(
            WorkspaceId: artifact.WorkspaceId,
            SchemaVersion: "1.0",
            UpdatedUtc: DateTimeOffset.UtcNow,
            Artifacts: artifacts);

        var indexRequest = new ArtifactStorageRequest(
            artifact.WorkspaceId,
            ArtifactStorageKinds.Other,
            "_index",
            "artifact-manifest-index.json",
            "application/json");

        await using var output = new MemoryStream();
        await JsonSerializer.SerializeAsync(output, updated, JsonOptions, cancellationToken).ConfigureAwait(false);
        output.Position = 0;

        await _artifactStorage.WriteAsync(indexRequest, output, cancellationToken).ConfigureAwait(false);

        return updated;
    }

    private static ArtifactManifestIndex Empty(string workspaceId) =>
        new(
            WorkspaceId: string.IsNullOrWhiteSpace(workspaceId) ? "default" : workspaceId,
            SchemaVersion: "1.0",
            UpdatedUtc: DateTimeOffset.UtcNow,
            Artifacts: Array.Empty<ArtifactManifestEntry>());
}
