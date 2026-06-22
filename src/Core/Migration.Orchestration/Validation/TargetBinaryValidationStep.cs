using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Domain.Models;
using Migration.Orchestration.Options;

namespace Migration.Orchestration.Validation;

public sealed class TargetBinaryValidationStep : IValidationStep
{
    private readonly ValidationOptions _options;

    public TargetBinaryValidationStep(IOptions<MigrationExecutionOptions> options)
    {
        _options = options.Value.Validation;
    }

    public Task<IReadOnlyList<ValidationIssue>> ValidateAsync(AssetWorkItem item, CancellationToken cancellationToken = default)
    {
        if (!_options.RequireBinaryForTargetWrites)
        {
            return Task.FromResult<IReadOnlyList<ValidationIssue>>(Array.Empty<ValidationIssue>());
        }

        var binary = item.TargetPayload?.Binary ?? item.SourceAsset?.Binary;
        var fallbackSource = ResolveFallbackSourceLocation(item);

        if (binary is null)
        {
            if (!string.IsNullOrWhiteSpace(fallbackSource))
            {
                return Task.FromResult<IReadOnlyList<ValidationIssue>>(Array.Empty<ValidationIssue>());
            }

            return Task.FromResult<IReadOnlyList<ValidationIssue>>(new[]
            {
                new ValidationIssue("target.binary_missing", "Target payload has no binary and no manifest source path/url/blob reference. A target write would create metadata-only or bad asset.")
            });
        }

        if (string.IsNullOrWhiteSpace(binary.SourceUri) && string.IsNullOrWhiteSpace(fallbackSource))
        {
            return Task.FromResult<IReadOnlyList<ValidationIssue>>(new[]
            {
                new ValidationIssue("target.binary_source_missing", "Target payload binary has no SourceUri/path/url and the manifest has no fallback source path/url/blob reference.")
            });
        }

        if (binary.Length is <= 0 && string.IsNullOrWhiteSpace(binary.SourceUri) && string.IsNullOrWhiteSpace(fallbackSource))
        {
            return Task.FromResult<IReadOnlyList<ValidationIssue>>(new[]
            {
                new ValidationIssue("target.binary_empty", "Target payload binary length is zero and no alternate source location is available.")
            });
        }

        return Task.FromResult<IReadOnlyList<ValidationIssue>>(Array.Empty<ValidationIssue>());
    }

    private static string? ResolveFallbackSourceLocation(AssetWorkItem item)
    {
        return FirstNonEmpty(
            item.Manifest.SourcePath,
            ReadString(item.Manifest.Columns,
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
            item.Manifest.SourceAssetId);
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

        return null;
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
}
