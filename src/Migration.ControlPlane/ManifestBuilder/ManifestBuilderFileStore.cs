using Microsoft.Extensions.Configuration;
using Migration.ControlPlane.Options;

namespace Migration.ControlPlane.ManifestBuilder;

public sealed class ManifestBuilderFileStore
{
    private readonly string _root;

    public ManifestBuilderFileStore(IConfiguration configuration)
    {
        var configuredRoot = configuration[$"{AdminApiOptions.SectionName}:StorageRoot"];

        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            configuredRoot = Path.Combine(AppContext.BaseDirectory, ".migration-control-plane");
        }

        _root = Path.Combine(configuredRoot, "manifest-builder");
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredManifestBuilderFile> SaveAsync(
        BuildSourceManifestResult result,
        CancellationToken cancellationToken = default)
    {
        var manifestId = $"manifest-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var safeFileName = MakeSafeFileName(result.FileName);
        var folder = Path.Combine(_root, manifestId);

        Directory.CreateDirectory(folder);

        var contentPath = Path.Combine(folder, safeFileName);
        await File.WriteAllTextAsync(contentPath, result.Content, cancellationToken).ConfigureAwait(false);

        var metadata = new StoredManifestBuilderFile(
            manifestId,
            result.SourceType,
            result.ServiceName,
            safeFileName,
            string.IsNullOrWhiteSpace(result.ContentType) ? "text/csv" : result.ContentType,
            result.RowCount,
            DateTimeOffset.UtcNow,
            contentPath);

        var metadataPath = Path.Combine(folder, "metadata.json");
        await File.WriteAllTextAsync(
            metadataPath,
            System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }),
            cancellationToken).ConfigureAwait(false);

        return metadata;
    }

    public StoredManifestBuilderFile? TryGet(string manifestId)
    {
        if (string.IsNullOrWhiteSpace(manifestId))
        {
            return null;
        }

        var safeManifestId = MakeSafeFileName(manifestId);
        var metadataPath = Path.Combine(_root, safeManifestId, "metadata.json");

        if (!File.Exists(metadataPath))
        {
            return null;
        }

        var json = File.ReadAllText(metadataPath);
        return System.Text.Json.JsonSerializer.Deserialize<StoredManifestBuilderFile>(json);
    }

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "manifest.csv" : cleaned;
    }
}

public sealed record StoredManifestBuilderFile(
    string ManifestId,
    string SourceType,
    string ServiceName,
    string FileName,
    string ContentType,
    int RowCount,
    DateTimeOffset CreatedUtc,
    string Path);
