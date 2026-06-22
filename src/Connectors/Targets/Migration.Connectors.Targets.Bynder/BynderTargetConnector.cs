using System.Collections;
using System.Reflection;
using Bynder.Sdk.Query.Upload;
using Bynder.Sdk.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Connectors.Targets.Bynder.Services;
using Migration.Domain.Models;

namespace Migration.Connectors.Targets.Bynder;

/// <summary>
/// Generic Phase 1 Bynder target connector.
///
/// This connector is registered by the worker runtime even when there is no global
/// appsettings Bynder section. For control-plane runs, it hydrates Bynder options from
/// job settings populated from the selected target credential set.
/// </summary>
public sealed class BynderTargetConnector : IAssetTargetConnector
{
    private static readonly HttpClient HttpClient = new();

    private readonly BynderOptions _configuredOptions;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<BynderTargetConnector> _logger;

    private static readonly HashSet<string> ReservedFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "id",
        "mediaId",
        "assetId",
        "bynderId",
        "name",
        "filename",
        "fileName",
        "originalFileName",
        "description",
        "tags",
        "keywords",
        "sourceUri",
        "downloadUrl",
        "url",
        "filePath",
        "path",
        "metapropertyOptions",
        "metadata"
    };

    public string Type => "Bynder";

    public BynderTargetConnector(
        IOptions<BynderOptions> options,
        IMemoryCache memoryCache,
        ILogger<BynderTargetConnector> logger)
    {
        _configuredOptions = options.Value;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<MigrationResult> UpsertAsync(
        MigrationJobDefinition job,
        AssetWorkItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(item);

        if (job.DryRun)
        {
            return new MigrationResult
            {
                WorkItemId = item.WorkItemId,
                Success = true,
                Message = "Dry run only. No Bynder target write was attempted."
            };
        }

        try
        {
            var runtimeOptions = ResolveRuntimeOptions(job, _configuredOptions);
            var bynderClient = ClientFactory.Create(runtimeOptions.Client);
            var assetResiliencyService = new AssetResiliencyService(bynderClient);
            var metapropertyOptionBuilderFactory = new MetapropertyOptionBuilderFactory(bynderClient, _memoryCache);

            var fileName = ResolveFileName(item);
            var binary = item.TargetPayload?.Binary ?? item.SourceAsset?.Binary;
            var source = ResolveSourceLocation(item);

            if (binary?.OpenReadAsync is null && string.IsNullOrWhiteSpace(source))
            {
                return Failure(
                    item,
                    "No source binary location was available. Expected SourceAsset.Binary stream/SourceUri, TargetPayload.Binary stream/SourceUri, Manifest.SourcePath, or a mapped sourceUri/downloadUrl/filePath/url field.");
            }

            await using var uploadStream = await OpenUploadStreamAsync(binary, source, cancellationToken).ConfigureAwait(false);
            var stream = uploadStream.Stream;
            var streamLength = TryGetLength(stream) ?? binary?.Length;

            if (streamLength is <= 0)
            {
                return Failure(item, $"Resolved source '{uploadStream.SourceDescription}' opened as an empty stream.");
            }

            var uploadQuery = await BuildUploadQueryAsync(item, fileName, runtimeOptions, metapropertyOptionBuilderFactory).ConfigureAwait(false);

            _logger.LogInformation(
                "Uploading work item {WorkItemId} to Bynder. File={FileName}; Bytes={Length}; Source={Source}; MetaPropertyCount={MetaPropertyCount}; TagCount={TagCount}",
                item.WorkItemId,
                fileName,
                streamLength,
                uploadStream.SourceDescription,
                uploadQuery.MetapropertyOptions?.Count ?? 0,
                uploadQuery.Tags?.Count ?? 0);

            var response = await assetResiliencyService
                .UploadFileAsync(stream, uploadQuery)
                .ConfigureAwait(false);

            var targetAssetId = ExtractTargetAssetId(response);

            if (string.IsNullOrWhiteSpace(targetAssetId))
            {
                return Failure(
                    item,
                    "Bynder upload completed but the SDK response did not expose a media/asset id. Refusing to mark work item succeeded.");
            }

            return new MigrationResult
            {
                WorkItemId = item.WorkItemId,
                Success = true,
                TargetAssetId = targetAssetId,
                Message = $"Bynder asset uploaded. TargetAssetId={targetAssetId}; MetadataFields={uploadQuery.MetapropertyOptions?.Count ?? 0}; Tags={uploadQuery.Tags?.Count ?? 0}"
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bynder upload failed for work item {WorkItemId}.", item.WorkItemId);

            return new MigrationResult
            {
                WorkItemId = item.WorkItemId,
                Success = false,
                Message = $"Bynder upload failed: {ex.Message}"
            };
        }
    }

    private async Task<UploadQuery> BuildUploadQueryAsync(
        AssetWorkItem item,
        string fileName,
        BynderOptions runtimeOptions,
        MetapropertyOptionBuilderFactory metapropertyOptionBuilderFactory)
    {
        var fields = item.TargetPayload?.Fields ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var description = ReadString(fields, "description", "Description")
            ?? ReadString(item.Manifest.Columns, "Description", "description")
            ?? item.TargetPayload?.Name
            ?? fileName;

        var mediaId = ReadString(fields, "mediaId", "MediaId", "id", "Id")
            ?? ReadString(item.Manifest.Columns, "Id", "id", "MediaId", "mediaId");

        var metapropertyOptions = await ResolveMetapropertyOptionsAsync(fields, metapropertyOptionBuilderFactory).ConfigureAwait(false);
        var tags = ResolveTags(fields, item.Manifest.Columns);

        LogMappedMetadata(item, fields, metapropertyOptions);

        var query = new UploadQuery
        {
            Filepath = fileName,
            Name = item.TargetPayload?.Name ?? ReadString(fields, "name", "Name") ?? fileName,
            OriginalFileName = fileName,
            Description = description,
            BrandId = runtimeOptions.BrandStoreId,
            Tags = tags,
            MetapropertyOptions = metapropertyOptions
        };

        if (!string.IsNullOrWhiteSpace(mediaId))
        {
            query.MediaId = mediaId;
        }

        return query;
    }

    /// <summary>
    /// Converts mapped target fields into Bynder metaproperty options.
    ///
    /// Supported mapping styles:
    /// 1. Preferred simple style: target = exact Bynder metaproperty display name.
    /// 2. Explicit style: target = meta:Exact Bynder metaproperty display name.
    /// 3. Pre-shaped style: target = metapropertyOptions/metadata dictionary.
    /// </summary>
    private async Task<IDictionary<string, IList<string>>> ResolveMetapropertyOptionsAsync(
        IReadOnlyDictionary<string, object?> fields,
        MetapropertyOptionBuilderFactory metapropertyOptionBuilderFactory)
    {
        var builder = await metapropertyOptionBuilderFactory.CreateBuilder().ConfigureAwait(false);

        AddPreShapedMetaproperties(builder, ReadObject(fields, "metapropertyOptions", "MetapropertyOptions", "metadata", "Metadata"));

        foreach (var field in fields)
        {
            if (field.Value is null)
            {
                continue;
            }

            var targetName = NormalizeMetapropertyTargetName(field.Key);

            if (string.IsNullOrWhiteSpace(targetName))
            {
                continue;
            }

            var values = ToStringList(field.Value);

            if (values.Count == 0)
            {
                continue;
            }

            try
            {
                builder[targetName] = values;
            }
            catch (BynderException)
            {
                // The field is not a Bynder metaproperty. That is okay for reserved/control fields,
                // but useful to log for migration mapping troubleshooting.
                if (!ReservedFieldNames.Contains(field.Key) && !field.Key.StartsWith("_", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Mapped field '{FieldName}' could not be resolved as a Bynder metaproperty. It will not be stamped.",
                        field.Key);
                }
            }
        }

        return builder.ToMetapropertyOptions();
    }

    private void AddPreShapedMetaproperties(IMetapropertyOptionBuilder builder, object? value)
    {
        if (value is null)
        {
            return;
        }

        if (value is IDictionary<string, object?> objectDictionary)
        {
            foreach (var item in objectDictionary)
            {
                var values = ToStringList(item.Value);
                if (values.Count > 0)
                {
                    builder[item.Key] = values;
                }
            }

            return;
        }

        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString();

                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var values = ToStringList(entry.Value);

                if (values.Count > 0)
                {
                    builder[key] = values;
                }
            }
        }
    }

    private static string? NormalizeMetapropertyTargetName(string fieldName)
    {
        if (ReservedFieldNames.Contains(fieldName))
        {
            return null;
        }

        if (fieldName.StartsWith("_", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (fieldName.StartsWith("meta:", StringComparison.OrdinalIgnoreCase))
        {
            return fieldName["meta:".Length..].Trim();
        }

        if (fieldName.StartsWith("metaproperty:", StringComparison.OrdinalIgnoreCase))
        {
            return fieldName["metaproperty:".Length..].Trim();
        }

        return fieldName.Trim();
    }

    private static List<string> ResolveTags(
        IReadOnlyDictionary<string, object?> fields,
        IReadOnlyDictionary<string, string> manifestColumns)
    {
        var tagsValue = ReadObject(fields, "tags", "Tags", "keywords", "Keywords")
            ?? ReadString(manifestColumns, "Tags", "tags", "Keywords", "keywords");

        return ToStringList(tagsValue)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void LogMappedMetadata(
        AssetWorkItem item,
        IReadOnlyDictionary<string, object?> fields,
        IDictionary<string, IList<string>> metapropertyOptions)
    {
        if (metapropertyOptions.Count == 0)
        {
            _logger.LogWarning(
                "No Bynder metaproperty options were produced for work item {WorkItemId}. Check that mapping target names match Bynder metaproperty display names.",
                item.WorkItemId);
            return;
        }

        _logger.LogInformation(
            "Prepared {Count} Bynder metaproperty fields for work item {WorkItemId}. Mapped target fields: {Fields}",
            metapropertyOptions.Count,
            item.WorkItemId,
            string.Join(", ", fields.Keys));
    }

    private static IList<string> ToStringList(object? value)
    {
        if (value is null)
        {
            return new List<string>();
        }

        if (value is string text)
        {
            return SplitMultiValue(text).ToList();
        }

        if (value is IEnumerable<string> strings)
        {
            return strings
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();
        }

        if (value is IEnumerable enumerable)
        {
            return enumerable
                .Cast<object?>()
                .Select(x => x?.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToList();
        }

        return new List<string> { value.ToString() ?? string.Empty };
    }

    private static IEnumerable<string> SplitMultiValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    private string ResolveFileName(AssetWorkItem item)
    {
        return item.TargetPayload?.Binary?.FileName
            ?? item.SourceAsset?.Binary?.FileName
            ?? ReadString(item.TargetPayload?.Fields, "filename", "fileName", "FileName", "name", "Name")
            ?? ReadString(item.Manifest.Columns, "Filename", "FileName", "filename", "Name", "name")
            ?? FileNameFromPath(item.TargetPayload?.Binary?.SourceUri)
            ?? FileNameFromPath(item.SourceAsset?.Binary?.SourceUri)
            ?? FileNameFromPath(item.Manifest.SourcePath)
            ?? $"{item.WorkItemId}.bin";
    }

    private static string? ResolveSourceLocation(AssetWorkItem item)
    {
        return item.TargetPayload?.Binary?.SourceUri
            ?? item.SourceAsset?.Binary?.SourceUri
            ?? ReadString(item.TargetPayload?.Fields,
                "sourceUri", "SourceUri", "sourceUrl", "SourceUrl", "downloadUrl", "DownloadUrl", "url", "Url",
                "blobUri", "BlobUri", "blobUrl", "BlobUrl", "blobName", "BlobName", "sourceBlobName", "SourceBlobName",
                "filePath", "FilePath", "filepath", "Path", "path", "relativePath", "RelativePath", "fullPath", "FullPath", "key", "Key", "objectKey", "ObjectKey")
            ?? item.Manifest.SourcePath
            ?? ReadString(item.Manifest.Columns,
                "SourceUri", "sourceUri", "source_uri", "SourceUrl", "sourceUrl", "source_url",
                "DownloadUrl", "downloadUrl", "download_url", "Url", "url", "URL",
                "BlobUri", "blobUri", "blob_uri", "BlobUrl", "blobUrl", "blob_url",
                "BlobName", "blobName", "blob_name", "SourceBlobName", "sourceBlobName", "source_blob_name",
                "FilePath", "filePath", "file_path", "filepath", "Path", "path",
                "RelativePath", "relativePath", "relative_path", "FullPath", "fullPath", "full_path",
                "Key", "key", "ObjectKey", "objectKey", "object_key")
            ?? item.Manifest.SourceAssetId;
    }

    private static async Task<UploadStreamLease> OpenUploadStreamAsync(
        AssetBinary? binary,
        string? source,
        CancellationToken cancellationToken)
    {
        if (binary?.OpenReadAsync is not null)
        {
            var stream = await binary.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            if (stream.CanSeek)
            {
                stream.Position = 0;
                return new UploadStreamLease(stream, source ?? binary.SourceUri ?? binary.FileName ?? "connector stream");
            }

            return await CopyToTemporarySeekableFileAsync(stream, source ?? binary.SourceUri ?? binary.FileName ?? "connector stream", cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new InvalidOperationException("No binary stream opener or source path/url was available.");
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            var remote = await HttpClient.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);
            if (remote.CanSeek)
            {
                return new UploadStreamLease(remote, source);
            }

            return await CopyToTemporarySeekableFileAsync(remote, source, cancellationToken).ConfigureAwait(false);
        }

        var localPath = source;

        if (Uri.TryCreate(source, UriKind.Absolute, out var fileUri)
            && fileUri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            localPath = fileUri.LocalPath;
        }

        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException($"Source file was not found: {localPath}", localPath);
        }

        var file = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1024 * 128, options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        return new UploadStreamLease(file, localPath);
    }

    private static async Task<UploadStreamLease> CopyToTemporarySeekableFileAsync(
        Stream sourceStream,
        string sourceDescription,
        CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "migration-bynder-upload-" + Guid.NewGuid().ToString("N") + ".bin");
        var destination = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, bufferSize: 1024 * 128, options: FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);

        try
        {
            await using (sourceStream.ConfigureAwait(false))
            {
                await sourceStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            }

            destination.Position = 0;
            return new UploadStreamLease(destination, sourceDescription + " (temporary seekable file)");
        }
        catch
        {
            await destination.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static long? TryGetLength(Stream stream)
    {
        try
        {
            return stream.CanSeek ? stream.Length : null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }

    private sealed class UploadStreamLease : IAsyncDisposable
    {
        public UploadStreamLease(Stream stream, string sourceDescription)
        {
            Stream = stream;
            SourceDescription = sourceDescription;
        }

        public Stream Stream { get; }
        public string SourceDescription { get; }

        public async ValueTask DisposeAsync()
        {
            await Stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string? ExtractTargetAssetId(object? response)
    {
        if (response is null)
        {
            return null;
        }

        foreach (var propertyName in new[] { "MediaId", "mediaId", "Id", "ID", "AssetId", "assetId", "Identifier", "PublicId" })
        {
            var property = response
                .GetType()
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            var value = property?.GetValue(response)?.ToString();

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
        return string.IsNullOrWhiteSpace(last) ? null : Uri.UnescapeDataString(last);
    }

    private static string? ReadString(
        IReadOnlyDictionary<string, object?>? values,
        params string[] keys)
    {
        return ReadObject(values, keys)?.ToString();
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
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static object? ReadObject(
        IReadOnlyDictionary<string, object?>? values,
        params string[] keys)
    {
        if (values is null)
        {
            return null;
        }

        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) && value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static BynderOptions ResolveRuntimeOptions(MigrationJobDefinition job, BynderOptions configuredOptions)
    {
        var baseUrl = GetSetting(job, "BynderBaseUrl", "TargetBaseUrl", "TargetCredential_BaseUrl")
            ?? configuredOptions.Client?.BaseUrl?.ToString();

        var clientId = GetSetting(job, "BynderClientId", "TargetClientId", "TargetCredential_ClientId")
            ?? configuredOptions.Client?.ClientId;

        var clientSecret = GetSetting(job, "BynderClientSecret", "TargetClientSecret", "TargetCredential_ClientSecret")
            ?? configuredOptions.Client?.ClientSecret;

        var scopes = GetSetting(job, "BynderScopes", "TargetScopes", "TargetCredential_Scopes")
            ?? configuredOptions.Client?.Scopes;

        var brandStoreId = GetSetting(job, "BynderBrandStoreId", "TargetBrandStoreId", "TargetCredential_BrandStoreId")
            ?? configuredOptions.BrandStoreId;

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(baseUrl)) missing.Add("BaseUrl");
        if (string.IsNullOrWhiteSpace(clientId)) missing.Add("ClientId");
        if (string.IsNullOrWhiteSpace(clientSecret)) missing.Add("ClientSecret");
        if (string.IsNullOrWhiteSpace(scopes)) missing.Add("Scopes");
        if (string.IsNullOrWhiteSpace(brandStoreId)) missing.Add("BrandStoreId");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Bynder target connector is missing required credential value(s): {string.Join(", ", missing)}. Expected job settings BynderBaseUrl/BynderClientId/BynderClientSecret/BynderScopes/BynderBrandStoreId or TargetCredential_* equivalents.");
        }

        return new BynderOptions
        {
            Client = new global::Bynder.Sdk.Settings.Configuration
            {
                BaseUrl = new Uri(baseUrl!, UriKind.Absolute),
                ClientId = clientId!,
                ClientSecret = clientSecret!,
                Scopes = scopes!
            },
            BrandStoreId = brandStoreId!
        };
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

    private static MigrationResult Failure(AssetWorkItem item, string message)
    {
        return new MigrationResult
        {
            WorkItemId = item.WorkItemId,
            Success = false,
            Message = message
        };
    }
}
