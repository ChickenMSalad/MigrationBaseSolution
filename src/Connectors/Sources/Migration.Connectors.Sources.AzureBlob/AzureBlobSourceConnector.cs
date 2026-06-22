using Migration.Application.Abstractions;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.AzureBlob;

public sealed class AzureBlobSourceConnector : IAssetSourceConnector
{
    public string Type => "AzureBlob";

    public Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(row);

        var sourceLocation = ResolveSourceLocation(row);
        var fileName = ResolveFileName(row, sourceLocation);
        var contentType = ReadString(row.Columns, "ContentType", "contentType", "MimeType", "mimeType", "Mime", "mime", "FileType", "fileType");
        var length = TryReadLength(row.Columns, "Length", "length", "Size", "size", "FileSize", "fileSize", "Bytes", "bytes", "ContentLength", "contentLength", "BlobSize", "blobSize");
        var checksum = ReadString(row.Columns, "Checksum", "checksum", "Hash", "hash", "MD5", "md5", "Sha256", "SHA256", "sha256", "ETag", "etag");

        return Task.FromResult(new AssetEnvelope
        {
            SourceAssetId = row.SourceAssetId ?? row.RowId,
            Path = row.SourcePath ?? sourceLocation,
            SourceType = ConnectorType.AzureBlob,
            Metadata = new Dictionary<string, string?>(row.Columns, StringComparer.OrdinalIgnoreCase),
            Binary = string.IsNullOrWhiteSpace(sourceLocation)
                ? null
                : new AssetBinary
                {
                    SourceUri = sourceLocation,
                    FileName = fileName,
                    ContentType = contentType,
                    Length = length,
                    Checksum = checksum
                }
        });
    }

    private static string? ResolveSourceLocation(ManifestRow row)
    {
        return FirstNonEmpty(
            row.SourcePath,
            ReadString(row.Columns,
                "SourceUri", "sourceUri", "source_uri",
                "SourceUrl", "sourceUrl", "source_url",
                "DownloadUrl", "downloadUrl", "download_url",
                "Url", "url", "URL",
                "BlobUri", "blobUri", "blob_uri",
                "BlobUrl", "blobUrl", "blob_url",
                "BlobName", "blobName", "blob_name",
                "SourceBlobName", "sourceBlobName", "source_blob_name",
                "FilePath", "filePath", "file_path", "filepath",
                "Path", "path",
                "RelativePath", "relativePath", "relative_path",
                "FullPath", "fullPath", "full_path",
                "Key", "key",
                "ObjectKey", "objectKey", "object_key"),
            row.SourceAssetId);
    }

    private static string? ResolveFileName(ManifestRow row, string? sourceLocation)
    {
        return FirstNonEmpty(
            ReadString(row.Columns,
                "FileName", "fileName", "filename", "Filename",
                "OriginalFileName", "originalFileName", "original_filename",
                "Name", "name", "Title", "title"),
            FileNameFromPath(sourceLocation),
            FileNameFromPath(row.SourcePath),
            row.SourceAssetId,
            row.RowId);
    }

    private static string? ReadString(IReadOnlyDictionary<string, string?> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        foreach (var key in keys)
        {
            var match = values.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match.Value))
            {
                return match.Value;
            }
        }

        return null;
    }

    private static long? TryReadLength(IReadOnlyDictionary<string, string?> values, params string[] keys)
    {
        var raw = ReadString(values, keys);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return long.TryParse(raw, out var parsed) ? parsed : null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? FileNameFromPath(string? pathOrUri)
    {
        if (string.IsNullOrWhiteSpace(pathOrUri))
        {
            return null;
        }

        var value = pathOrUri.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            value = uri.IsFile ? uri.LocalPath : uri.AbsolutePath;
        }

        value = value.Replace('\\', '/');
        var last = value.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(last))
        {
            return null;
        }

        return Uri.UnescapeDataString(last);
    }
}
