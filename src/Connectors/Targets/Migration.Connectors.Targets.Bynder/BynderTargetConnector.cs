using System.Collections;
using System.Reflection;
using Bynder.Sdk.Query.Upload;
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
/// Responsibilities:
/// - consume the canonical target payload created by the mapper;
/// - upload the binary through the existing AssetResiliencyService;
/// - translate mapped fields into Bynder metaproperty option IDs;
/// - stamp description/tags/metaproperties during upload;
/// - refuse false success when Bynder returns no asset id.
/// </summary>
public sealed class BynderTargetConnector : IAssetTargetConnector
{
    private static readonly HttpClient HttpClient = new();

    private readonly AssetResiliencyService _assetResiliencyService;
    private readonly MetapropertyOptionBuilderFactory _metapropertyOptionBuilderFactory;
    private readonly BynderOptions _options;
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
        AssetResiliencyService assetResiliencyService,
        MetapropertyOptionBuilderFactory metapropertyOptionBuilderFactory,
        IOptions<BynderOptions> options,
        ILogger<BynderTargetConnector> logger)
    {
        _assetResiliencyService = assetResiliencyService;
        _metapropertyOptionBuilderFactory = metapropertyOptionBuilderFactory;
        _options = options.Value;
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
            var fileName = ResolveFileName(item);
            var source = ResolveSourceLocation(item);

            if (string.IsNullOrWhiteSpace(source))
            {
                return Failure(
                    item,
                    "No source binary location was available. Expected TargetPayload.Binary.SourceUri, SourceAsset.Binary.SourceUri, Manifest.SourcePath, or a mapped sourceUri/downloadUrl/filePath/url field.");
            }

            await using var stream = await OpenSeekableStreamAsync(source, cancellationToken).ConfigureAwait(false);

            if (stream.Length <= 0)
            {
                return Failure(item, $"Resolved source '{source}' opened as an empty stream.");
            }

            var uploadQuery = await BuildUploadQueryAsync(item, fileName).ConfigureAwait(false);

            _logger.LogInformation(
                "Uploading work item {WorkItemId} to Bynder. File={FileName}; Bytes={Length}; Source={Source}; MetaPropertyCount={MetaPropertyCount}; TagCount={TagCount}",
                item.WorkItemId,
                fileName,
                stream.Length,
                source,
                uploadQuery.MetapropertyOptions?.Count ?? 0,
                uploadQuery.Tags?.Count ?? 0);

            var response = await _assetResiliencyService
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

    private async Task<UploadQuery> BuildUploadQueryAsync(AssetWorkItem item, string fileName)
    {
        var fields = item.TargetPayload?.Fields ?? new Dictionary<string, object?>();

        var description =
            ReadString(fields, "description", "Description") ??
            ReadString(item.Manifest.Columns, "Description", "description") ??
            item.TargetPayload?.Name ??
            fileName;

        var mediaId =
            ReadString(fields, "mediaId", "MediaId", "id", "Id") ??
            ReadString(item.Manifest.Columns, "Id", "id", "MediaId", "mediaId");

        var metapropertyOptions = await ResolveMetapropertyOptionsAsync(fields).ConfigureAwait(false);
        var tags = ResolveTags(fields, item.Manifest.Columns);

        LogMappedMetadata(item, fields, metapropertyOptions);

        var query = new UploadQuery
        {
            Filepath = fileName,
            Name = item.TargetPayload?.Name ?? ReadString(fields, "name", "Name") ?? fileName,
            OriginalFileName = fileName,
            Description = description,
            BrandId = _options.BrandStoreId,
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
    ///
    /// 1. Preferred simple style:
    ///    source: WebDamColumn
    ///    target: Exact Bynder metaproperty display name
    ///
    /// 2. Explicit style:
    ///    target: meta:Exact Bynder metaproperty display name
    ///
    /// 3. Pre-shaped style:
    ///    target: metapropertyOptions
    ///    value: Dictionary&lt;string, object&gt; where keys are Bynder display names or ids.
    ///
    /// The existing MetapropertyOptionBuilderFactory resolves display names into the Bynder
    /// IDs expected by UploadQuery.MetapropertyOptions.
    /// </summary>
    private async Task<IDictionary<string, IList<string>>> ResolveMetapropertyOptionsAsync(
        IReadOnlyDictionary<string, object?> fields)
    {
        var builder = await _metapropertyOptionBuilderFactory.CreateBuilder().ConfigureAwait(false);

        // Pre-shaped metadata dictionaries, when present.
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
                if (!ReservedFieldNames.Contains(field.Key) &&
                    !field.Key.StartsWith("_", StringComparison.OrdinalIgnoreCase))
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

        // Default behavior: any non-reserved mapped target field is treated as
        // a Bynder metaproperty display name.
        return fieldName.Trim();
    }

    private static List<string> ResolveTags(
        IReadOnlyDictionary<string, object?> fields,
        IReadOnlyDictionary<string, string?> manifestColumns)
    {
        var tagsValue =
            ReadObject(fields, "tags", "Tags", "keywords", "Keywords") ??
            ReadString(manifestColumns, "Tags", "tags", "Keywords", "keywords");

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
                   "sourceUri",
                   "SourceUri",
                   "downloadUrl",
                   "DownloadUrl",
                   "url",
                   "Url",
                   "filePath",
                   "FilePath",
                   "filepath",
                   "Path",
                   "path")
               ?? item.Manifest.SourcePath
               ?? ReadString(item.Manifest.Columns,
                   "sourceUri",
                   "SourceUri",
                   "downloadUrl",
                   "DownloadUrl",
                   "url",
                   "Url",
                   "filePath",
                   "FilePath",
                   "filepath",
                   "Path",
                   "path");
    }

    private static async Task<MemoryStream> OpenSeekableStreamAsync(
        string source,
        CancellationToken cancellationToken)
    {
        var destination = new MemoryStream();

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            await using var remote = await HttpClient.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);
            await remote.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            destination.Position = 0;
            return destination;
        }

        var localPath = source;

        if (Uri.TryCreate(source, UriKind.Absolute, out var fileUri) &&
            fileUri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            localPath = fileUri.LocalPath;
        }

        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException($"Source file was not found: {localPath}", localPath);
        }

        await using var file = File.OpenRead(localPath);
        await file.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        destination.Position = 0;
        return destination;
    }

    private static string? ExtractTargetAssetId(object? response)
    {
        if (response is null)
        {
            return null;
        }

        foreach (var propertyName in new[]
                 {
                     "MediaId",
                     "mediaId",
                     "Id",
                     "ID",
                     "AssetId",
                     "assetId",
                     "Identifier",
                     "PublicId"
                 })
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

        if (Uri.TryCreate(pathOrUri, UriKind.Absolute, out var uri))
        {
            return Path.GetFileName(uri.LocalPath);
        }

        return Path.GetFileName(pathOrUri);
    }

    private static string? ReadString(
        IReadOnlyDictionary<string, object?>? values,
        params string[] keys)
    {
        return ReadObject(values, keys)?.ToString();
    }

    private static string? ReadString(
        IReadOnlyDictionary<string, string?>? values,
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
