using Migration.Application.Abstractions;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Targets.Bynder;

public sealed class BynderSourceConnector : IAssetSourceConnector
{
    public string Type => "Bynder";

    public Task<AssetEnvelope> GetAssetAsync(
        MigrationJobDefinition job,
        ManifestRow row,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(row);

        var metadata = new Dictionary<string, string>(row.Columns, StringComparer.OrdinalIgnoreCase);

        var sourceUri =
            ReadString(metadata, "sourceUri", "SourceUri") ??
            ReadString(metadata, "downloadUrl", "DownloadUrl") ??
            ReadString(metadata, "url", "Url") ??
            ReadString(metadata, "originalUrl", "OriginalUrl") ??
            ReadString(metadata, "filePath", "FilePath") ??
            ReadString(metadata, "filepath", "Path", "path") ??
            row.SourcePath;

        var fileName =
            ReadString(metadata, "fileName", "FileName") ??
            ReadString(metadata, "filename", "Filename") ??
            ReadString(metadata, "originalFileName", "OriginalFileName") ??
            ReadString(metadata, "name", "Name") ??
            FileNameFromPath(sourceUri) ??
            FileNameFromPath(row.SourcePath) ??
            $"{row.RowId}.bin";

        var contentType =
            ReadString(metadata, "contentType", "ContentType") ??
            ReadString(metadata, "mimeType", "MimeType");

        var length = ReadLong(metadata, "size", "Size", "fileSize", "FileSize", "sizeBytes", "SizeBytes");

        return Task.FromResult(new AssetEnvelope
        {
            SourceAssetId =
                row.SourceAssetId ??
                ReadString(metadata, "id", "Id", "mediaId", "MediaId", "assetId", "AssetId", "bynderId", "BynderId") ??
                row.RowId,
            ExternalId = ReadString(metadata, "id", "Id", "mediaId", "MediaId", "assetId", "AssetId", "bynderId", "BynderId"),
            Path = row.SourcePath ?? sourceUri,
            SourceType = ConnectorType.Bynder,
            Metadata = metadata,
            Binary = string.IsNullOrWhiteSpace(sourceUri)
                ? null
                : new AssetBinary
                {
                    SourceUri = sourceUri,
                    FileName = fileName,
                    ContentType = contentType,
                    Length = length
                }
        });
    }

    private static string? ReadString(
        IReadOnlyDictionary<string, string>? values,
        params string[] keys)
    {
        if (values is null)
        {
            return null;
        }

        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static long? ReadLong(
        IReadOnlyDictionary<string, string> values,
        params string[] keys)
    {
        var text = ReadString(values, keys);

        if (long.TryParse(text, out var value))
        {
            return value;
        }

        return null;
    }

    private static string? FileNameFromPath(string? pathOrUri)
    {
        if (string.IsNullOrWhiteSpace(pathOrUri))
        {
            return null;
        }

        if (Uri.TryCreate(pathOrUri, UriKind.Absolute, out var uri))
        {
            var fromUri = Path.GetFileName(uri.LocalPath);
            return string.IsNullOrWhiteSpace(fromUri) ? null : fromUri;
        }

        var fromPath = Path.GetFileName(pathOrUri);
        return string.IsNullOrWhiteSpace(fromPath) ? null : fromPath;
    }
}
