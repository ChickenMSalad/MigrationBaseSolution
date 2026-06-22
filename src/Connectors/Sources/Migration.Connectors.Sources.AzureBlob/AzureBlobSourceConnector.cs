using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Migration.Application.Abstractions;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.AzureBlob;

public sealed class AzureBlobSourceConnector : IAssetSourceConnector
{
    private readonly ILogger<AzureBlobSourceConnector>? _logger;

    public AzureBlobSourceConnector(ILogger<AzureBlobSourceConnector>? logger = null)
    {
        _logger = logger;
    }

    public string Type => "AzureBlob";

    public async Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(row);

        var sourceAssetId = FirstNonEmpty(row.SourceAssetId, ReadString(row.Columns, "source_asset_id", "SourceAssetId", "sourceAssetId"), row.RowId) ?? row.RowId;
        var metadata = new Dictionary<string, string?>(row.Columns, StringComparer.OrdinalIgnoreCase);

        var connectionString = GetSetting(job,
            "AzureBlobSourceConnectionString",
            "SourceCredential_ConnectionString",
            "SourceCredential_AzureBlobConnectionString",
            "SourceAzureBlobConnectionString",
            "SourceConnectionString",
            "AzureBlobConnectionString",
            "IntermediateStorageConnectionString",
            "TargetCredential_ConnectionString",
            "AzureBlobTargetConnectionString",
            "TargetConnectionString")
            ?? job.ConnectionString;

        var containerName = GetSetting(job,
            "AzureBlobSourceContainer",
            "AzureBlobSourceContainerName",
            "SourceCredential_ContainerName",
            "SourceCredential_Container",
            "SourceAzureBlobContainer",
            "SourceContainer",
            "SourceContainerName",
            "AzureBlobContainer",
            "AzureBlobContainerName",
            "IntermediateStorageContainer",
            "TargetCredential_ContainerName",
            "TargetCredential_Container",
            "AzureBlobTargetContainer",
            "TargetContainer")
            ?? ReadString(row.Columns, "Container", "container", "ContainerName", "containerName", "BlobContainer", "blobContainer");

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(containerName))
        {
            var sourceLocation = ResolveManifestSourceLocation(row, includeSourceAssetIdFallback: false);
            return new AssetEnvelope
            {
                SourceAssetId = sourceAssetId,
                Path = row.SourcePath ?? sourceLocation,
                SourceType = ConnectorType.AzureBlob,
                Metadata = metadata,
                Binary = string.IsNullOrWhiteSpace(sourceLocation)
                    ? null
                    : new AssetBinary
                    {
                        SourceUri = sourceLocation,
                        FileName = ResolveFileName(row, sourceLocation),
                        ContentType = ReadString(row.Columns, "ContentType", "contentType", "MimeType", "mimeType"),
                        Length = TryReadLength(row.Columns, "Length", "length", "Size", "size", "FileSize", "fileSize", "Bytes", "bytes"),
                        Checksum = ReadString(row.Columns, "Checksum", "checksum", "Hash", "hash", "MD5", "md5", "ETag", "etag")
                    }
            };
        }

        var container = new BlobContainerClient(connectionString, containerName);
        var blob = await ResolveBlobAsync(container, sourceAssetId, row, cancellationToken).ConfigureAwait(false);

        if (blob is null)
        {
            _logger?.LogWarning(
                "Azure Blob source could not resolve a blob for SourceAssetId={SourceAssetId}. Tag lookup source_asset_id did not match and no path/blob-name fallback matched.",
                sourceAssetId);

            return new AssetEnvelope
            {
                SourceAssetId = sourceAssetId,
                Path = row.SourcePath,
                SourceType = ConnectorType.AzureBlob,
                Metadata = metadata
            };
        }

        var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var blobName = blob.Name;
        var sourceUri = $"azureblob://{container.Name}/{blobName}";

        metadata["azure_blob_container"] = container.Name;
        metadata["azure_blob_name"] = blobName;
        metadata["azure_blob_uri"] = blob.Uri.ToString();
        metadata["source_asset_id"] = sourceAssetId;

        return new AssetEnvelope
        {
            SourceAssetId = sourceAssetId,
            Path = blobName,
            SourceType = ConnectorType.AzureBlob,
            Metadata = metadata,
            Binary = new AssetBinary
            {
                SourceUri = sourceUri,
                FileName = ResolveFileName(row, blobName),
                ContentType = FirstNonEmpty(properties.Value.ContentType, ReadString(row.Columns, "ContentType", "contentType", "MimeType", "mimeType")),
                Length = properties.Value.ContentLength,
                Checksum = FirstNonEmpty(properties.Value.ETag.ToString(), ReadString(row.Columns, "Checksum", "checksum", "Hash", "hash", "MD5", "md5", "ETag", "etag")),
                OpenReadAsync = async token => await blob.OpenReadAsync(new BlobOpenReadOptions(allowModifications: false), token).ConfigureAwait(false)
            }
        };
    }

    private async Task<BlobClient?> ResolveBlobAsync(BlobContainerClient container, string sourceAssetId, ManifestRow row, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(sourceAssetId))
        {
            var tagged = await FindBySourceAssetIdTagAsync(container, sourceAssetId, cancellationToken).ConfigureAwait(false);
            if (tagged is not null)
            {
                return tagged;
            }

            // Existing intermediate blobs may have source_asset_id as regular blob metadata, not an Azure Blob index tag.
            // Metadata cannot be queried server-side, so use this as a compatibility fallback after tag lookup and before path lookup.
            var metadataMatched = await FindBySourceAssetIdMetadataAsync(container, sourceAssetId, row, cancellationToken).ConfigureAwait(false);
            if (metadataMatched is not null)
            {
                return metadataMatched;
            }
        }

        foreach (var candidate in EnumerateBlobNameCandidates(row, container.Name))
        {
            var blob = container.GetBlobClient(candidate);
            if (await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger?.LogInformation("Azure Blob source resolved blob by fallback path. SourceAssetId={SourceAssetId}; BlobName={BlobName}", sourceAssetId, candidate);
                return blob;
            }
        }

        return null;
    }

    private async Task<BlobClient?> FindBySourceAssetIdTagAsync(BlobContainerClient container, string sourceAssetId, CancellationToken cancellationToken)
    {
        var escaped = sourceAssetId.Replace("'", "''", StringComparison.Ordinal);
        var expression = $"\"source_asset_id\" = '{escaped}'";
        BlobClient? match = null;
        var count = 0;

        await foreach (var item in container.FindBlobsByTagsAsync(expression, cancellationToken).ConfigureAwait(false))
        {
            count++;
            match ??= container.GetBlobClient(item.BlobName);
            if (count > 1)
            {
                break;
            }
        }

        if (count == 1 && match is not null)
        {
            _logger?.LogInformation("Azure Blob source resolved blob by index tag source_asset_id={SourceAssetId}; BlobName={BlobName}", sourceAssetId, match.Name);
            return match;
        }

        if (count > 1)
        {
            _logger?.LogWarning("Azure Blob source found multiple blobs with index tag source_asset_id={SourceAssetId}. Refusing non-unique tag match; falling back to path lookup.", sourceAssetId);
        }

        return null;
    }


    private async Task<BlobClient?> FindBySourceAssetIdMetadataAsync(BlobContainerClient container, string sourceAssetId, ManifestRow row, CancellationToken cancellationToken)
    {
        var prefixes = EnumerateMetadataSearchPrefixes(row).ToArray();
        BlobClient? match = null;
        var count = 0;
        var scanned = 0;

        foreach (var prefix in prefixes)
        {
            await foreach (var item in container.GetBlobsAsync(BlobTraits.Metadata, BlobStates.None, prefix, cancellationToken).ConfigureAwait(false))
            {
                scanned++;
                if (item.Metadata is null)
                {
                    continue;
                }

                if (!TryMetadataEquals(item.Metadata, sourceAssetId, "source_asset_id", "SourceAssetId", "sourceAssetId"))
                {
                    continue;
                }

                count++;
                match ??= container.GetBlobClient(item.Name);
                if (count > 1)
                {
                    break;
                }
            }

            if (count > 0)
            {
                break;
            }
        }

        if (count == 1 && match is not null)
        {
            _logger?.LogInformation("Azure Blob source resolved blob by metadata source_asset_id={SourceAssetId}; BlobName={BlobName}; Scanned={Scanned}", sourceAssetId, match.Name, scanned);
            return match;
        }

        if (count > 1)
        {
            _logger?.LogWarning("Azure Blob source found multiple blobs with metadata source_asset_id={SourceAssetId}. Refusing non-unique metadata match; falling back to path lookup.", sourceAssetId);
        }
        else
        {
            _logger?.LogInformation("Azure Blob source metadata fallback did not find source_asset_id={SourceAssetId}. Scanned={Scanned}", sourceAssetId, scanned);
        }

        return null;
    }

    private static IEnumerable<string?> EnumerateMetadataSearchPrefixes(ManifestRow row)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in new[]
        {
            PrefixFromPath(row.SourcePath),
            PrefixFromPath(ReadString(row.Columns, "BlobName", "blobName", "blob_name")),
            PrefixFromPath(ReadString(row.Columns, "SourceBlobName", "sourceBlobName", "source_blob_name")),
            PrefixFromPath(ReadString(row.Columns, "RelativePath", "relativePath", "relative_path")),
            PrefixFromPath(ReadString(row.Columns, "FullPath", "fullPath", "full_path")),
            PrefixFromPath(ReadString(row.Columns, "FilePath", "filePath", "file_path", "filepath")),
            PrefixFromPath(ReadString(row.Columns, "Path", "path")),
            ReadString(row.Columns, "SourceFolder", "sourceFolder", "source_folder", "Folder", "folder", "Directory", "directory")
        })
        {
            var normalized = NormalizePrefix(candidate);
            if (!string.IsNullOrWhiteSpace(normalized) && seen.Add(normalized))
            {
                yield return normalized;
            }
        }

        // Last resort: whole-container metadata scan. Existing legacy staged blobs may have no useful path in the manifest.
        if (seen.Add(string.Empty))
        {
            yield return null;
        }
    }

    private static string? PrefixFromPath(string? pathOrUri)
    {
        var blobName = NormalizeBlobName(pathOrUri, string.Empty);
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return null;
        }

        var slash = blobName.LastIndexOf('/');
        return slash <= 0 ? null : blobName[..(slash + 1)];
    }

    private static string? NormalizePrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = NormalizeBlobName(value, string.Empty)?.Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.EndsWith("/", StringComparison.Ordinal) ? normalized : normalized + "/";
    }

    private static bool TryMetadataEquals(IDictionary<string, string> metadata, string expectedValue, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (metadata.TryGetValue(key, out var value) && string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var entry in metadata)
        {
            if (keys.Any(key => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                && string.Equals(entry.Value, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateBlobNameCandidates(ManifestRow row, string containerName)
    {
        var candidates = new[]
        {
            ResolveManifestSourceLocation(row, includeSourceAssetIdFallback: false),
            ReadString(row.Columns, "BlobName", "blobName", "blob_name"),
            ReadString(row.Columns, "SourceBlobName", "sourceBlobName", "source_blob_name"),
            ReadString(row.Columns, "RelativePath", "relativePath", "relative_path"),
            ReadString(row.Columns, "FullPath", "fullPath", "full_path"),
            ReadString(row.Columns, "FilePath", "filePath", "file_path", "filepath"),
            ReadString(row.Columns, "Path", "path"),
            ReadString(row.Columns, "Key", "key", "ObjectKey", "objectKey", "object_key")
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in candidates)
        {
            var normalized = NormalizeBlobName(raw, containerName);
            if (!string.IsNullOrWhiteSpace(normalized) && seen.Add(normalized))
            {
                yield return normalized;
            }
        }
    }

    private static string? ResolveManifestSourceLocation(ManifestRow row, bool includeSourceAssetIdFallback)
    {
        var result = FirstNonEmpty(
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
                "ObjectKey", "objectKey", "object_key"));

        return includeSourceAssetIdFallback ? FirstNonEmpty(result, row.SourceAssetId) : result;
    }

    private static string? NormalizeBlobName(string? value, string containerName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var candidate = value.Trim().Replace('\\', '/');
        if (candidate.StartsWith("azureblob://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(candidate, UriKind.Absolute, out var azureBlobUri))
            {
                candidate = azureBlobUri.AbsolutePath.TrimStart('/');
            }
        }
        else if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            candidate = uri.AbsolutePath.TrimStart('/');
        }

        if (candidate.StartsWith(containerName + "/", StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[(containerName.Length + 1)..];
        }

        return Uri.UnescapeDataString(candidate.TrimStart('/'));
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

    private static string? GetSetting(MigrationJobDefinition job, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (job.Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
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
