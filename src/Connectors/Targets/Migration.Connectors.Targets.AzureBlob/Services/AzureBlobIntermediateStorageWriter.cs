using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Targets.AzureBlob.Models;

namespace Migration.Connectors.Targets.AzureBlob.Services;

/// <summary>
/// Writes an intermediate-storage mapping profile to Azure Blob Storage.
/// Supports binary-only, blob index tags, and sidecar metadata JSON.
/// </summary>
public sealed class AzureBlobIntermediateStorageWriter
{
    private static readonly Regex TokenRegex = new(@"\{(?<name>[^}]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobIntermediateStorageWriter> _logger;

    public AzureBlobIntermediateStorageWriter(
        BlobContainerClient container,
        ILogger<AzureBlobIntermediateStorageWriter> logger)
    {
        _container = container;
        _logger = logger;
    }

    public async Task<AzureBlobIntermediateWriteResult> WriteAsync(
        Stream binary,
        IReadOnlyDictionary<string, string?> rowValues,
        IntermediateStorageOptions storage,
        string? contentType = null,
        string? rootPrefix = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binary);
        ArgumentNullException.ThrowIfNull(rowValues);
        ArgumentNullException.ThrowIfNull(storage);

        var blobName = BuildBlobName(storage.BlobNameTemplate, rowValues, rootPrefix);
        var blobTags = storage.BinaryOnly || !storage.WriteBlobTags
            ? new Dictionary<string, string>()
            : BuildBlobTags(storage.TagRules, rowValues);

        var metadata = BuildSafeBlobMetadata(rowValues);
        metadata["migration_mapping_type"] = "intermediate";

        var client = _container.GetBlobClient(blobName);

        if (binary.CanSeek)
        {
            binary.Position = 0;
        }

        _logger.LogInformation("Uploading intermediate binary blob {BlobName}", blobName);

        await client.UploadAsync(binary, new BlobUploadOptions
        {
            HttpHeaders = string.IsNullOrWhiteSpace(contentType)
                ? null
                : new BlobHttpHeaders { ContentType = contentType },
            Metadata = metadata,
            Tags = blobTags.Count == 0 ? null : blobTags
        }, cancellationToken).ConfigureAwait(false);

        string? metadataBlobName = null;

        if (!storage.BinaryOnly && storage.WriteMetadataJson)
        {
            metadataBlobName = BuildBlobName(storage.MetadataJsonPathTemplate, rowValues, rootPrefix, fallbackTemplate: "metadata/{assetId}.json");
            var document = BuildMetadataDocument(storage.MetadataRules, rowValues, blobName, blobTags);
            var json = JsonSerializer.Serialize(document, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            });

            await using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var metadataClient = _container.GetBlobClient(metadataBlobName);

            _logger.LogInformation("Uploading intermediate metadata blob {BlobName}", metadataBlobName);

            await metadataClient.UploadAsync(jsonStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" },
                Tags = blobTags.Count == 0 ? null : blobTags
            }, cancellationToken).ConfigureAwait(false);
        }

        return new AzureBlobIntermediateWriteResult(blobName, metadataBlobName, blobTags);
    }

    public static string BuildBlobName(
        string? template,
        IReadOnlyDictionary<string, string?> rowValues,
        string? rootPrefix = null,
        string fallbackTemplate = "{assetId}")
    {
        var effectiveTemplate = string.IsNullOrWhiteSpace(template) ? fallbackTemplate : template!;
        var expanded = ExpandTemplate(effectiveTemplate, rowValues);
        expanded = expanded.Replace('\\', '/').Trim('/');

        if (string.IsNullOrWhiteSpace(expanded))
        {
            expanded = TryGet(rowValues, "assetId") ?? TryGet(rowValues, "Asset ID") ?? Guid.NewGuid().ToString("N");
        }

        expanded = string.Join('/', expanded.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SanitizeBlobSegment));

        if (!string.IsNullOrWhiteSpace(rootPrefix))
        {
            return $"{rootPrefix.Trim().Trim('/')}/{expanded}";
        }

        return expanded;
    }

    private static string ExpandTemplate(string template, IReadOnlyDictionary<string, string?> rowValues)
    {
        return TokenRegex.Replace(template, match =>
        {
            var token = match.Groups["name"].Value;
            return TryGet(rowValues, token) ?? string.Empty;
        });
    }

    private static Dictionary<string, string> BuildBlobTags(
        IEnumerable<BlobTagRule>? rules,
        IReadOnlyDictionary<string, string?> rowValues)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rules ?? [])
        {
            if (string.IsNullOrWhiteSpace(rule.SourceField) || string.IsNullOrWhiteSpace(rule.TagName))
            {
                continue;
            }

            var raw = TryGet(rowValues, rule.SourceField);
            var value = ApplyTransform(raw, rule.Transform);

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            tags[SanitizeTagKey(rule.TagName)] = SanitizeTagValue(value);
        }

        return tags;
    }

    private static Dictionary<string, object?> BuildMetadataDocument(
        IEnumerable<MetadataJsonRule>? rules,
        IReadOnlyDictionary<string, string?> rowValues,
        string blobName,
        IReadOnlyDictionary<string, string> blobTags)
    {
        var root = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["_migration"] = new Dictionary<string, object?>
            {
                ["storageProvider"] = "AzureBlob",
                ["binaryBlobName"] = blobName,
                ["createdUtc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                ["blobTags"] = blobTags
            }
        };

        foreach (var rule in rules ?? [])
        {
            if (string.IsNullOrWhiteSpace(rule.SourceField) || string.IsNullOrWhiteSpace(rule.JsonPath))
            {
                continue;
            }

            var value = ApplyTransform(TryGet(rowValues, rule.SourceField), rule.Transform);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            SetJsonPath(root, rule.JsonPath, value);
        }

        return root;
    }

    private static void SetJsonPath(Dictionary<string, object?> root, string jsonPath, string value)
    {
        var parts = jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return;
        }

        Dictionary<string, object?> current = root;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            if (!current.TryGetValue(part, out var child) || child is not Dictionary<string, object?> childMap)
            {
                childMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                current[part] = childMap;
            }

            current = childMap;
        }

        current[parts[^1]] = value;
    }

    private static Dictionary<string, string> BuildSafeBlobMetadata(IReadOnlyDictionary<string, string?> rowValues)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in new[] { "assetId", "Asset ID", "webdam_id", "Filename", "FileName", "Name" })
        {
            var value = TryGet(rowValues, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                metadata[SanitizeMetadataKey(key)] = SanitizeMetadataValue(value);
            }
        }

        return metadata;
    }

    private static string? TryGet(IReadOnlyDictionary<string, string?> values, string key)
    {
        if (values.TryGetValue(key, out var exact))
        {
            return exact;
        }

        var match = values.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(match.Key))
        {
            return match.Value;
        }

        var normalizedKey = NormalizeKey(key);
        match = values.FirstOrDefault(x => string.Equals(NormalizeKey(x.Key), normalizedKey, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(match.Key) ? null : match.Value;
    }

    private static string ApplyTransform(string? value, string? transform)
    {
        var current = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(transform))
        {
            return current;
        }

        return transform.Trim().ToLowerInvariant() switch
        {
            "trim" => current.Trim(),
            "lower" => current.ToLowerInvariant(),
            "upper" => current.ToUpperInvariant(),
            "empty-to-null" => string.IsNullOrWhiteSpace(current) ? string.Empty : current,
            "split:semicolon" => string.Join(",", current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
            "split:comma" => string.Join(",", current.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
            _ => current
        };
    }

    private static string NormalizeKey(string key) =>
        Regex.Replace(key.Trim().ToLowerInvariant(), @"[^a-z0-9]+", string.Empty, RegexOptions.CultureInvariant);

    private static string SanitizeBlobSegment(string value)
    {
        var safe = Regex.Replace(value.Trim(), @"[\x00-\x1F\x7F]+", "", RegexOptions.CultureInvariant);
        safe = safe.Replace("..", "_");
        return string.IsNullOrWhiteSpace(safe) ? "_" : safe;
    }

    private static string SanitizeTagKey(string value)
    {
        var safe = Regex.Replace(value.Trim(), @"[^A-Za-z0-9_\-]", "_", RegexOptions.CultureInvariant);
        return safe.Length > 128 ? safe[..128] : safe;
    }

    private static string SanitizeTagValue(string value)
    {
        var safe = Regex.Replace(value.Trim(), @"[\r\n\t]", " ", RegexOptions.CultureInvariant);
        return safe.Length > 256 ? safe[..256] : safe;
    }

    private static string SanitizeMetadataKey(string value)
    {
        var safe = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9_]", "_", RegexOptions.CultureInvariant);
        if (safe.Length == 0 || !char.IsLetter(safe[0]) && safe[0] != '_')
        {
            safe = "m_" + safe;
        }

        return safe.Length > 128 ? safe[..128] : safe;
    }

    private static string SanitizeMetadataValue(string value)
    {
        var safe = Regex.Replace(value.Trim(), @"[\r\n\t]", " ", RegexOptions.CultureInvariant);
        return safe.Length > 8192 ? safe[..8192] : safe;
    }
}

public sealed record AzureBlobIntermediateWriteResult(
    string BinaryBlobName,
    string? MetadataBlobName,
    IReadOnlyDictionary<string, string> BlobTags);
