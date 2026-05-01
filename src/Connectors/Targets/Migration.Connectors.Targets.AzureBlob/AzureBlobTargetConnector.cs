using System.Net.Http;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Connectors.Targets.AzureBlob.Configuration;
using Migration.Domain.Models;

namespace Migration.Connectors.Targets.AzureBlob;

public sealed class AzureBlobTargetConnector : IAssetTargetConnector
{
    private readonly AzureBlobTargetOptions _options;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<AzureBlobTargetConnector> _logger;

    public AzureBlobTargetConnector(
        IOptions<AzureBlobTargetOptions> options,
        ILogger<AzureBlobTargetConnector> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _options = options.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string Type => "AzureBlob";

    public async Task<MigrationResult> UpsertAsync(MigrationJobDefinition job, AssetWorkItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(item);

        var connectionString = GetSetting(job, "AzureBlobTargetConnectionString", "AzureBlobConnectionString", "TargetConnectionString")
            ?? _options.ConnectionString
            ?? job.ConnectionString;

        var containerName = GetSetting(job, "AzureBlobTargetContainer", "AzureBlobContainer", "TargetContainer")
            ?? _options.ContainerName;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Fail(item, "Azure Blob target connection string is missing. Set AzureBlobTarget:ConnectionString or job setting AzureBlobTargetConnectionString.");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            return Fail(item, "Azure Blob target container is missing. Set AzureBlobTarget:ContainerName or job setting AzureBlobTargetContainer.");
        }

        var binary = item.TargetPayload?.Binary ?? item.SourceAsset?.Binary;
        if (binary is null || string.IsNullOrWhiteSpace(binary.SourceUri))
        {
            return Fail(item, "Target payload has no binary SourceUri to upload to Azure Blob.");
        }

        var intermediateStorage = ResolveIntermediateStorageProfile(job);
        var naming = ResolveBlobNaming(job, item, binary, intermediateStorage);
        var overwrite = GetBool(job, _options.Overwrite, "AzureBlobTargetOverwrite", "AzureBlobOverwrite", "Overwrite");
        var writeSidecar = intermediateStorage?.WriteMetadataJson ?? GetBool(job, _options.WriteMetadataSidecar, "AzureBlobWriteMetadataSidecar", "WriteMetadataSidecar");
        var createContainer = GetBool(job, _options.CreateContainerIfMissing, "AzureBlobCreateContainerIfMissing", "CreateContainerIfMissing");

        var container = new BlobContainerClient(connectionString, containerName);
        if (createContainer)
        {
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var blob = container.GetBlobClient(naming.BinaryBlobName);
        if (!overwrite && await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return Fail(item, $"Azure Blob already exists and overwrite is disabled: {naming.BinaryBlobName}");
        }

        await using var sourceStream = await OpenBinaryStreamAsync(binary.SourceUri, cancellationToken).ConfigureAwait(false);
        if (sourceStream.CanSeek && sourceStream.Length <= 0)
        {
            return Fail(item, $"Binary stream is empty. SourceUri={binary.SourceUri}");
        }

        var headers = new BlobHttpHeaders
        {
            ContentType = FirstNonEmpty(binary.ContentType, GuessContentType(naming.BinaryFileName))
        };

        var blobMetadata = BuildBlobMetadata(job, item, intermediateStorage);
        var blobTags = BuildBlobTags(job, item, intermediateStorage);

        _logger.LogInformation("Uploading work item {WorkItemId} to Azure Blob {BlobName}.", item.WorkItemId, naming.BinaryBlobName);

        await blob.UploadAsync(sourceStream, new BlobUploadOptions
        {
            HttpHeaders = headers,
            Metadata = blobMetadata.Count == 0 ? null : blobMetadata,
            Tags = blobTags.Count == 0 ? null : blobTags
        }, cancellationToken).ConfigureAwait(false);

        if (writeSidecar)
        {
            var sidecarResult = await WriteMetadataSidecarAsync(container, job, item, binary, naming, overwrite, intermediateStorage, cancellationToken).ConfigureAwait(false);
            if (!sidecarResult.Success)
            {
                return sidecarResult;
            }
        }

        return new MigrationResult
        {
            WorkItemId = item.WorkItemId,
            Success = true,
            TargetAssetId = $"azureblob:{naming.BinaryBlobName}",
            Message = writeSidecar
                ? $"Uploaded binary{(blobTags.Count > 0 ? ", tags" : string.Empty)} and metadata sidecar: {naming.BinaryBlobName}; {naming.MetadataBlobName}"
                : $"Uploaded binary{(blobTags.Count > 0 ? " and tags" : string.Empty)}: {naming.BinaryBlobName}"
        };
    }

    private async Task<MigrationResult> WriteMetadataSidecarAsync(
        BlobContainerClient container,
        MigrationJobDefinition job,
        AssetWorkItem item,
        AssetBinary binary,
        BlobNaming naming,
        bool overwrite,
        IntermediateStorageProfile? intermediateStorage,
        CancellationToken cancellationToken)
    {
        var sidecarBlob = container.GetBlobClient(naming.MetadataBlobName);
        if (!overwrite && await sidecarBlob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return Fail(item, $"Azure Blob metadata sidecar already exists and overwrite is disabled: {naming.MetadataBlobName}");
        }

        var document = BuildSidecarDocument(job, item, binary, naming, intermediateStorage);
        var jsonOptions = new JsonSerializerOptions { WriteIndented = GetBool(job, _options.PrettyPrintMetadataSidecar, "AzureBlobPrettyPrintMetadataSidecar", "PrettyPrintMetadataSidecar") };
        var json = JsonSerializer.Serialize(document, jsonOptions);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        await sidecarBlob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/json; charset=utf-8" },
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["migration_job"] = SafeBlobMetadataValue(job.JobName),
                ["migration_work_item"] = SafeBlobMetadataValue(item.WorkItemId),
                ["source_asset_id"] = SafeBlobMetadataValue(item.SourceAsset?.SourceAssetId ?? item.Manifest.SourceAssetId ?? string.Empty),
                ["sidecar_for"] = SafeBlobMetadataValue(naming.BinaryBlobName)
            }
        }, cancellationToken).ConfigureAwait(false);

        return new MigrationResult { WorkItemId = item.WorkItemId, Success = true, TargetAssetId = $"azureblob:{naming.MetadataBlobName}" };
    }

    private BlobNaming ResolveBlobNaming(MigrationJobDefinition job, AssetWorkItem item, AssetBinary binary, IntermediateStorageProfile? intermediateStorage)
    {
        var uniqueIdField = GetSetting(job, "AzureBlobUniqueIdField", "UniqueIdField") ?? _options.UniqueIdField;
        var fileNameField = GetSetting(job, "AzureBlobFileNameField", "FileNameField") ?? _options.FileNameField;
        var folderPathField = GetSetting(job, "AzureBlobFolderPathField", "FolderPathField") ?? _options.SourceFolderPathField;

        var uniqueId = FirstNonEmpty(
            GetValue(item, uniqueIdField),
            item.Manifest.SourceAssetId,
            item.SourceAsset?.SourceAssetId,
            item.SourceAsset?.ExternalId,
            item.WorkItemId) ?? item.WorkItemId;

        var originalFileName = FirstNonEmpty(
            GetSetting(job, "AzureBlobTargetFileName", "TargetFileName"),
            GetValue(item, fileNameField),
            GetValue(item, "File Name"),
            GetValue(item, "filename"),
            GetValue(item, "file_name"),
            binary.FileName,
            FileNameFromUri(binary.SourceUri),
            uniqueId) ?? $"{uniqueId}.bin";

        originalFileName = SanitizePathSegment(Path.GetFileName(originalFileName));

        var rootFolder = FirstNonEmpty(
            intermediateStorage?.RootFolder,
            GetSetting(job, "AzureBlobTargetRootFolder", "AzureBlobRootFolder", "TargetRootFolder"),
            _options.RootFolderPath,
            _options.FolderPath);

        var preserveSourceFolderPath = intermediateStorage?.PreserveSourceFolderPath
            ?? GetBool(job, _options.PreserveSourceFolderPath, "AzureBlobPreserveSourceFolderPath", "PreserveSourceFolderPath");
        var sourceFolderPath = preserveSourceFolderPath
            ? FirstNonEmpty(GetValue(item, folderPathField), item.SourceAsset?.Path)
            : null;

        var safeUniqueId = SanitizePathSegment(uniqueId);
        var tokens = CreateTokens(item, safeUniqueId, originalFileName);

        var binaryTemplate = FirstNonEmpty(intermediateStorage?.BinaryFileNameTemplate, GetSetting(job, "AzureBlobBinaryFileNameTemplate", "BinaryFileNameTemplate"), _options.BinaryFileNameTemplate) ?? "{uniqueid}_{filename}";
        var metadataTemplate = FirstNonEmpty(intermediateStorage?.MetadataFileNameTemplate, GetSetting(job, "AzureBlobMetadataFileNameTemplate", "MetadataFileNameTemplate"), _options.MetadataFileNameTemplate) ?? "{uniqueid}.metadata.json";

        var binaryFileName = SanitizePathSegment(ApplyTemplate(binaryTemplate, tokens));
        var metadataFileName = SanitizePathSegment(ApplyTemplate(metadataTemplate, tokens));
        if (!metadataFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            metadataFileName += ".json";
        }

        var blobFolder = CombinePath(rootFolder, sourceFolderPath);

        return new BlobNaming
        {
            UniqueId = safeUniqueId,
            OriginalFileName = originalFileName,
            BinaryFileName = binaryFileName,
            MetadataFileName = metadataFileName,
            BinaryBlobName = CombinePath(blobFolder, binaryFileName) ?? binaryFileName,
            MetadataBlobName = CombinePath(blobFolder, metadataFileName) ?? metadataFileName,
            FolderPath = blobFolder ?? string.Empty
        };
    }

    private Dictionary<string, object?> BuildSidecarDocument(MigrationJobDefinition job, AssetWorkItem item, AssetBinary binary, BlobNaming naming, IntermediateStorageProfile? intermediateStorage)
    {
        var mode = GetSetting(job, "AzureBlobMetadataSidecarMode", "MetadataSidecarMode") ?? _options.MetadataSidecarMode;
        var includeEmpty = GetBool(job, _options.IncludeEmptyMetadataValues, "AzureBlobIncludeEmptyMetadataValues", "IncludeEmptyMetadataValues");
        var includeColumns = ParseColumnList(GetSetting(job, "AzureBlobMetadataIncludeColumns", "MetadataIncludeColumns") ?? _options.MetadataIncludeColumns);
        var excludeColumns = ParseColumnList(GetSetting(job, "AzureBlobMetadataExcludeColumns", "MetadataExcludeColumns") ?? _options.MetadataExcludeColumns);

        var document = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["migration"] = new Dictionary<string, object?>
            {
                ["jobName"] = job.JobName,
                ["workItemId"] = item.WorkItemId,
                ["sourceType"] = job.SourceType,
                ["targetType"] = job.TargetType,
                ["createdUtc"] = DateTimeOffset.UtcNow
            },
            ["source"] = new Dictionary<string, object?>
            {
                ["sourceAssetId"] = item.SourceAsset?.SourceAssetId ?? item.Manifest.SourceAssetId,
                ["externalId"] = item.SourceAsset?.ExternalId,
                ["sourcePath"] = item.SourceAsset?.Path,
                ["manifestRowId"] = item.Manifest.RowId
            },
            ["binary"] = new Dictionary<string, object?>
            {
                ["uniqueId"] = naming.UniqueId,
                ["originalFileName"] = naming.OriginalFileName,
                ["storedFileName"] = naming.BinaryFileName,
                ["storedBlobName"] = naming.BinaryBlobName,
                ["metadataFileName"] = naming.MetadataFileName,
                ["metadataBlobName"] = naming.MetadataBlobName,
                ["folderPath"] = naming.FolderPath,
                ["contentType"] = binary.ContentType,
                ["length"] = binary.Length,
                ["checksum"] = binary.Checksum,
                ["sourceUri"] = binary.SourceUri
            }
        };

        if (ModeIncludes(mode, "ManifestOnly", "All"))
        {
            document["manifest"] = FilterDictionary(item.Manifest.Columns, includeColumns, excludeColumns, includeEmpty);
        }

        if (ModeIncludes(mode, "MappedOnly", "All") && item.TargetPayload is not null)
        {
            document["mappedFields"] = FilterDictionary(item.TargetPayload.Fields, includeColumns, excludeColumns, includeEmpty);
        }

        if (ModeIncludes(mode, "SourceEnvelopeOnly", "All") && item.SourceAsset is not null)
        {
            document["sourceMetadata"] = FilterDictionary(item.SourceAsset.Metadata, includeColumns, excludeColumns, includeEmpty);
        }

        if (intermediateStorage is not null)
        {
            document["mappingProfile"] = new Dictionary<string, object?>
            {
                ["mappingType"] = "intermediate",
                ["storageMode"] = intermediateStorage.StorageMode
            };

            document["intermediateMetadata"] = intermediateStorage.MetadataFields.Count > 0
                ? BuildMappedDocument(item, intermediateStorage.MetadataFields)
                : FilterDictionary(item.Manifest.Columns, includeColumns, excludeColumns, includeEmpty);
        }

        return document;
    }

    private Dictionary<string, string> BuildBlobMetadata(MigrationJobDefinition job, AssetWorkItem item, IntermediateStorageProfile? intermediateStorage)
    {
        if (intermediateStorage is not null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        if (!GetBool(job, _options.WriteBlobMetadata, "AzureBlobWriteBlobMetadata", "WriteBlobMetadata"))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["migration_job"] = SafeBlobMetadataValue(job.JobName),
            ["migration_work_item"] = SafeBlobMetadataValue(item.WorkItemId),
            ["source_type"] = SafeBlobMetadataValue(job.SourceType),
            ["source_asset_id"] = SafeBlobMetadataValue(item.SourceAsset?.SourceAssetId ?? item.Manifest.SourceAssetId ?? string.Empty)
        };

        if (GetBool(job, _options.IncludeMappedFieldsAsBlobMetadata, "AzureBlobIncludeMappedFieldsAsBlobMetadata", "IncludeMappedFieldsAsBlobMetadata") && item.TargetPayload is not null)
        {
            foreach (var field in item.TargetPayload.Fields)
            {
                AddBlobMetadata(metadata, $"mapped_{field.Key}", field.Value);
            }
        }

        if (GetBool(job, _options.IncludeSourceColumnsAsBlobMetadata, "AzureBlobIncludeSourceColumnsAsBlobMetadata", "IncludeSourceColumnsAsBlobMetadata"))
        {
            foreach (var column in item.Manifest.Columns)
            {
                AddBlobMetadata(metadata, $"manifest_{column.Key}", column.Value);
            }
        }

        return metadata;
    }

    private Dictionary<string, string> BuildBlobTags(MigrationJobDefinition job, AssetWorkItem item, IntermediateStorageProfile? intermediateStorage)
    {
        if (intermediateStorage?.WriteBlobTags != true)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in intermediateStorage.TagFields)
        {
            if (tags.Count >= 10)
            {
                _logger.LogWarning("Azure Blob index tags are limited to 10. Remaining tag mappings were skipped for work item {WorkItemId}.", item.WorkItemId);
                break;
            }

            var value = GetValue(item, field.SourceField);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            AddBlobTag(tags, field.TargetName, value);
        }

        return tags;
    }

    private static Dictionary<string, object?> BuildMappedDocument(AssetWorkItem item, IEnumerable<FieldMapping> fields)
    {
        var document = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            var value = GetValue(item, field.SourceField);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            document[field.TargetName] = value;
        }

        return document;
    }

    private void AddBlobTag(IDictionary<string, string> tags, string key, string value)
    {
        var sanitizedKey = SafeBlobTagKey(key);
        if (string.IsNullOrWhiteSpace(sanitizedKey)) return;

        var sanitizedValue = SafeBlobTagValue(value);
        if (string.IsNullOrWhiteSpace(sanitizedValue)) return;

        tags[sanitizedKey] = sanitizedValue;
    }

    private string SafeBlobTagValue(string value)
    {
        var sanitized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        sanitized = new string(sanitized.Select(ch => ch <= 127 ? ch : '?').ToArray());
        return sanitized.Length <= 256 ? sanitized : sanitized[..256];
    }

    private static string SafeBlobTagKey(string key)
    {
        var sb = new StringBuilder(key.Length);
        foreach (var ch in key)
        {
            sb.Append(char.IsLetterOrDigit(ch) || ch is '_' or '-' or '.' or '/' or ':' or '=' ? ch : '_');
        }

        var sanitized = sb.ToString().Trim('_');
        return sanitized.Length <= 128 ? sanitized : sanitized[..128];
    }

    private async Task<Stream> OpenBinaryStreamAsync(string sourceUri, CancellationToken cancellationToken)
    {
        if (Uri.TryCreate(sourceUri, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            var client = _httpClientFactory?.CreateClient(nameof(AzureBlobTargetConnector)) ?? new HttpClient();
            var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }

        var localPath = Uri.TryCreate(sourceUri, UriKind.Absolute, out uri) && uri.IsFile ? uri.LocalPath : sourceUri;
        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException($"Binary source file not found: {localPath}", localPath);
        }

        return new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1024 * 128, useAsync: true);
    }

    private static IntermediateStorageProfile? ResolveIntermediateStorageProfile(MigrationJobDefinition job)
    {
        var json = GetSetting(
            job,
            "MappingProfileJson",
            "MappingArtifactJson",
            "MappingJson",
            "IntermediateStorageMappingJson",
            "AzureBlobIntermediateStorageJson");

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var mappingType = GetString(root, "mappingType", "type", "profileType");
            var storage = TryGetObject(root, "intermediateStorage", out var intermediateStorage)
                ? intermediateStorage
                : root;

            if (!string.IsNullOrWhiteSpace(mappingType) &&
                !mappingType.Equals("intermediate", StringComparison.OrdinalIgnoreCase) &&
                !TryGetObject(root, "intermediateStorage", out _))
            {
                return null;
            }

            var storageMode = FirstNonEmpty(GetString(storage, "storageMode", "mode"), "binaryWithMetadataJson") ?? "binaryWithMetadataJson";
            var profile = new IntermediateStorageProfile
            {
                StorageMode = storageMode,
                RootFolder = GetString(storage, "rootFolder", "rootFolderPath", "folderPath", "prefix"),
                BinaryFileNameTemplate = GetString(storage, "binaryFileNameTemplate", "fileNameTemplate", "blobNameTemplate"),
                MetadataFileNameTemplate = GetString(storage, "metadataFileNameTemplate", "metadataJsonFileNameTemplate", "sidecarFileNameTemplate"),
                PreserveSourceFolderPath = GetBool(storage, "preserveSourceFolderPath", "preserveFolders", "preserveSourcePath")
            };

            profile.TagFields.AddRange(ReadFieldMappings(storage, "tagFields", "tags", "tagMappings", "blobTags"));
            profile.MetadataFields.AddRange(ReadFieldMappings(storage, "metadataFields", "metadataMappings", "jsonMetadataFields", "sidecarFields"));

            return profile;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<FieldMapping> ReadFieldMappings(JsonElement source, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetArray(source, propertyName, out var array))
            {
                continue;
            }

            foreach (var element in array.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    var field = element.GetString();
                    if (!string.IsNullOrWhiteSpace(field))
                    {
                        yield return new FieldMapping(field, field);
                    }
                    continue;
                }

                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var sourceField = GetString(element, "sourceField", "source", "manifestField", "field", "column", "name");
                var targetName = FirstNonEmpty(
                    GetString(element, "targetName", "targetField", "tagName", "jsonName", "metadataName", "name", "as"),
                    sourceField);

                if (!string.IsNullOrWhiteSpace(sourceField) && !string.IsNullOrWhiteSpace(targetName))
                {
                    yield return new FieldMapping(sourceField, targetName);
                }
            }
        }
    }

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static bool? GetBool(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.True) return true;
            if (value.ValueKind == JsonValueKind.False) return false;
            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed)) return parsed;
        }

        return null;
    }

    private static bool TryGetObject(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetArray(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        value = default;
        return false;
    }

    private static Dictionary<string, object?> FilterDictionary<TValue>(IDictionary<string, TValue> source, HashSet<string> includeColumns, HashSet<string> excludeColumns, bool includeEmpty)
    {
        return source
            .Where(x => ShouldIncludeColumn(x.Key, x.Value, includeColumns, excludeColumns, includeEmpty))
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => NormalizeJsonValue(x.Value), StringComparer.OrdinalIgnoreCase);
    }

    private static bool ShouldIncludeColumn<TValue>(string key, TValue value, HashSet<string> includeColumns, HashSet<string> excludeColumns, bool includeEmpty)
    {
        if (excludeColumns.Contains(key)) return false;
        if (includeColumns.Count > 0 && !includeColumns.Contains(key)) return false;
        if (includeEmpty) return true;
        return !string.IsNullOrWhiteSpace(value?.ToString());
    }

    private static object? NormalizeJsonValue(object? value)
    {
        if (value is null) return null;
        if (value is string text) return text;
        if (value is System.Collections.IEnumerable values && value is not string)
        {
            var list = new List<object?>();
            foreach (var part in values)
            {
                list.Add(part);
            }
            return list;
        }
        return value;
    }

    private static bool ModeIncludes(string mode, params string[] accepted)
    {
        if (mode.Equals("None", StringComparison.OrdinalIgnoreCase)) return false;
        return accepted.Any(x => mode.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<string, string> CreateTokens(AssetWorkItem item, string uniqueId, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var basename = string.IsNullOrWhiteSpace(extension) ? originalFileName : originalFileName[..^extension.Length];
        var assetName = FirstNonEmpty(GetValue(item, "Asset Name"), item.TargetPayload?.Name, basename) ?? basename;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["uniqueid"] = uniqueId,
            ["id"] = uniqueId,
            ["filename"] = originalFileName,
            ["basename"] = SanitizePathSegment(basename),
            ["extension"] = extension.TrimStart('.'),
            ["assetname"] = SanitizePathSegment(assetName),
            ["rowid"] = SanitizePathSegment(item.WorkItemId)
        };
    }

    private static string ApplyTemplate(string template, IDictionary<string, string> tokens)
    {
        var result = template;
        foreach (var token in tokens)
        {
            result = result.Replace("{" + token.Key + "}", token.Value, StringComparison.OrdinalIgnoreCase);
        }
        return result;
    }

    private void AddBlobMetadata(IDictionary<string, string> metadata, string key, object? value)
    {
        if (value is null) return;
        var sanitizedKey = SafeBlobMetadataKey(key);
        if (string.IsNullOrWhiteSpace(sanitizedKey)) return;
        metadata[sanitizedKey] = SafeBlobMetadataValue(FormatMetadataValue(value));
    }

    private static string FormatMetadataValue(object value)
    {
        if (value is string text) return text;
        if (value is System.Collections.IEnumerable values)
        {
            var parts = new List<string>();
            foreach (var part in values)
            {
                if (part is not null) parts.Add(part.ToString() ?? string.Empty);
            }
            return string.Join(";", parts);
        }
        return value.ToString() ?? string.Empty;
    }

    private string SafeBlobMetadataValue(string value)
    {
        var max = Math.Max(1, _options.MaxBlobMetadataValueLength);
        var sanitized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        sanitized = new string(sanitized.Select(ch => ch <= 127 ? ch : '?').ToArray());
        return sanitized.Length <= max ? sanitized : sanitized[..max];
    }

    private static string SafeBlobMetadataKey(string key)
    {
        var sb = new StringBuilder(key.Length);
        foreach (var ch in key)
        {
            sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
        }

        var sanitized = sb.ToString().Trim('_');
        if (string.IsNullOrWhiteSpace(sanitized)) return string.Empty;
        if (!char.IsLetter(sanitized[0]) && sanitized[0] != '_') sanitized = "m_" + sanitized;
        return sanitized.Length <= 128 ? sanitized : sanitized[..128];
    }

    private static string? GetValue(AssetWorkItem item, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        if (item.TargetPayload?.Fields.TryGetValue(name, out var fieldValue) == true && !string.IsNullOrWhiteSpace(fieldValue?.ToString())) return fieldValue.ToString();
        if (item.Manifest.Columns.TryGetValue(name, out var manifestValue) && !string.IsNullOrWhiteSpace(manifestValue)) return manifestValue;
        if (item.SourceAsset?.Metadata.TryGetValue(name, out var metadataValue) == true && !string.IsNullOrWhiteSpace(metadataValue)) return metadataValue;

        return null;
    }

    private static string? GetSetting(MigrationJobDefinition job, params string[] names)
    {
        foreach (var name in names)
        {
            if (job.Settings.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }

    private static bool GetBool(MigrationJobDefinition job, bool defaultValue, params string[] names)
    {
        var configured = GetSetting(job, names);
        return bool.TryParse(configured, out var parsed) ? parsed : defaultValue;
    }

    private static string? CombinePath(params string?[] parts)
    {
        var clean = parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .SelectMany(part => part!.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(SanitizePathSegment)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return clean.Length == 0 ? null : string.Join('/', clean);
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(value.Select(ch => invalid.Contains(ch) || ch == ':' ? '_' : ch));
        return string.IsNullOrWhiteSpace(sanitized) ? "asset" : sanitized;
    }

    private static string? FileNameFromUri(string? sourceUri)
    {
        if (string.IsNullOrWhiteSpace(sourceUri)) return null;
        if (Uri.TryCreate(sourceUri, UriKind.Absolute, out var uri)) return Path.GetFileName(uri.LocalPath);
        return Path.GetFileName(sourceUri);
    }

    private static HashSet<string> ParseColumnList(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string? GuessContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".tif" or ".tiff" => "image/tiff",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }

    private static MigrationResult Fail(AssetWorkItem item, string message)
    {
        return new MigrationResult { WorkItemId = item.WorkItemId, Success = false, Message = message };
    }

    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

    private sealed class IntermediateStorageProfile
    {
        public required string StorageMode { get; init; }
        public string? RootFolder { get; init; }
        public string? BinaryFileNameTemplate { get; init; }
        public string? MetadataFileNameTemplate { get; init; }
        public bool? PreserveSourceFolderPath { get; init; }
        public List<FieldMapping> TagFields { get; } = new();
        public List<FieldMapping> MetadataFields { get; } = new();

        public bool WriteBlobTags => StorageMode.Equals("binaryWithTags", StringComparison.OrdinalIgnoreCase)
            || StorageMode.Equals("binaryWithTagsAndMetadataJson", StringComparison.OrdinalIgnoreCase)
            || StorageMode.Equals("tags", StringComparison.OrdinalIgnoreCase);

        public bool WriteMetadataJson => StorageMode.Equals("binaryWithMetadataJson", StringComparison.OrdinalIgnoreCase)
            || StorageMode.Equals("binaryWithTagsAndMetadataJson", StringComparison.OrdinalIgnoreCase)
            || StorageMode.Equals("metadataJson", StringComparison.OrdinalIgnoreCase)
            || StorageMode.Equals("json", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record FieldMapping(string SourceField, string TargetName);

    private sealed class BlobNaming
    {
        public required string UniqueId { get; init; }
        public required string OriginalFileName { get; init; }
        public required string BinaryFileName { get; init; }
        public required string MetadataFileName { get; init; }
        public required string BinaryBlobName { get; init; }
        public required string MetadataBlobName { get; init; }
        public required string FolderPath { get; init; }
    }
}
