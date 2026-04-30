using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Migration.Connectors.Sources.WebDam.Clients;
using Migration.Connectors.Sources.WebDam.Models;

namespace Migration.Connectors.Sources.WebDam.Services;

public sealed class WebDamExportService
{
    private readonly WebDamApiClient _apiClient;
    private readonly ILogger<WebDamExportService> _logger;

    public WebDamExportService(
        WebDamApiClient apiClient,
        ILogger<WebDamExportService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static bool HasProperty(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
    private async Task CollectAssetsForFolderAsync(
        string folderId,
        ICollection<WebDamAssetDto> assets,
        CancellationToken cancellationToken)
    {
        var offset = 0;

        while (true)
        {
            var page = await _apiClient.GetFolderContentsAsync(
                folderId,
                offset,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (page.Items.Count == 0)
            {
                _logger.LogDebug("No asset-page items returned for FolderId={FolderId}", folderId);
                return;
            }

            var addedThisPage = 0;

            foreach (var item in page.Items)
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var type = TryGetProperty(item, "type");

                if (string.Equals(type, "asset", StringComparison.OrdinalIgnoreCase) || HasProperty(item, "filename"))
                {
                    var asset = JsonSerializer.Deserialize<WebDamAssetDto>(item.GetRawText());
                    if (asset is not null)
                    {
                        assets.Add(asset);
                        addedThisPage++;
                    }
                }
            }

            _logger.LogInformation(
                "FolderId={FolderId}, page returned {ItemsCount} items, assets added this page={AddedThisPage}",
                folderId,
                page.Items.Count,
                addedThisPage);

            offset += page.Items.Count;

            if (offset >= page.TotalCount)
            {
                break;
            }
        }
    }

    private static IEnumerable<JsonElement> FindChildFolderElements(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in property.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var type = TryGetProperty(item, "type");
                if (string.Equals(type, "folder", StringComparison.OrdinalIgnoreCase))
                {
                    yield return item;
                }
            }
        }
    }

    private async Task DiscoverFoldersRecursivelyAsync(
    string folderId,
    IDictionary<string, WebDamFolderDto> folders,
    CancellationToken cancellationToken)
    {
        using var doc = await _apiClient.GetFolderInfoRawAsync(folderId, cancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;

        _logger.LogDebug("Inspecting folder {FolderId}: {Json}", folderId, root.GetRawText());

        foreach (var child in FindChildFolderElements(root))
        {
            var folder = JsonSerializer.Deserialize<WebDamFolderDto>(child.GetRawText());
            if (folder?.Id is null)
            {
                continue;
            }

            if (!folders.ContainsKey(folder.Id))
            {
                folder.Parent ??= folderId;
                folders[folder.Id] = folder;

                _logger.LogInformation(
                    "Discovered child folder Id={FolderId}, Parent={ParentId}, Name={Name}",
                    folder.Id,
                    folder.Parent,
                    folder.Name);

                await DiscoverFoldersRecursivelyAsync(folder.Id, folders, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task<WebDamExportResult> ExportAllAssetsAsync(CancellationToken cancellationToken = default)
    {
        var folders = new Dictionary<string, WebDamFolderDto>(StringComparer.OrdinalIgnoreCase);
        var assets = new List<WebDamAssetDto>();

        var roots = await _apiClient.GetTopLevelFoldersAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Total top-level folders returned: {FolderCount}", roots.Count);

        foreach (var root in roots.Where(r => !string.IsNullOrWhiteSpace(r.Id)))
        {
            folders[root.Id!] = root;
            await DiscoverFoldersRecursivelyAsync(root.Id!, folders, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Total folders discovered after recursion: {FolderCount}", folders.Count);

        foreach (var folderId in folders.Keys)
        {
            await CollectAssetsForFolderAsync(folderId, assets, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Total assets collected: {AssetCount}", assets.Count);

        var folderPaths = BuildFolderPaths(folders);

        var assetRows = assets
            .Where(a => !string.IsNullOrWhiteSpace(a.Id))
            .Select(a => new WebDamAssetExportRow
            {
                AssetId = a.Id!,
                FileName = a.Filename ?? string.Empty,
                Name = a.Name,
                SizeBytes = WebDamApiClient.ParseSizeToBytes(a.Filesize),
                FileType = a.Filetype,
                FolderId = a.Folder?.Id ?? string.Empty,
                FolderPath = GetFolderPath(a.Folder?.Id, folderPaths)
            })
            .ToList();

        _logger.LogInformation("Loading XMP schema...");
        var xmpSchema = await _apiClient.GetXmpSchemaAsync(cancellationToken).ConfigureAwait(false);

        var metadataDisplayNameMap = xmpSchema
            .Where(x => !string.IsNullOrWhiteSpace(x.Field))
            .ToDictionary(
                x => x.Field!,
                x => string.IsNullOrWhiteSpace(x.Label) ? x.Name ?? x.Field! : x.Label!,
                StringComparer.OrdinalIgnoreCase);

        var multiValueFields = BuildMultiValueFieldSet(xmpSchema);

        _logger.LogInformation(
            "Detected {Count} multi-valued metadata fields in WebDam schema.",
            multiValueFields.Count);

        var metadataSchemaRows = xmpSchema
            .Where(x => !string.IsNullOrWhiteSpace(x.Field))
            .Select(x => new WebDamMetadataSchemaExportRow
            {
                Field = x.Field!,
                Name = x.Name,
                Label = x.Label,
                Status = x.Status,
                Searchable = x.Searchable,
                Position = x.Position,
                Type = x.Type,
                PossibleValues = x.Values is null || x.Values.Count == 0
                    ? null
                    : string.Join("; ", x.Values)
            })
            .OrderBy(x => x.Label ?? x.Name ?? x.Field, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var metadataByAssetId = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var batch in Chunk(assetRows.Select(x => x.AssetId), 50))
        {
            _logger.LogInformation("Loading metadata batch of {BatchCount} assets...", batch.Count);

            var batchResult = await _apiClient.GetXmpMetadataBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            foreach (var pair in batchResult)
            {
                metadataByAssetId[pair.Key] = pair.Value;
            }
        }

        var metadataRows = assetRows
            .Select(a => new WebDamMetadataExportRow
            {
                AssetId = a.AssetId,
                Metadata = metadataByAssetId.TryGetValue(a.AssetId, out var metadata)
                    ? NormalizeMetadataForExport(metadata, multiValueFields)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            })
            .ToList();

        _logger.LogInformation(
            "Export build complete. Assets={AssetCount}, MetadataRows={MetadataCount}, SchemaRows={SchemaCount}",
            assetRows.Count,
            metadataRows.Count,
            metadataSchemaRows.Count);

        return new WebDamExportResult
        {
            Assets = assetRows,
            MetadataRows = metadataRows,
            MetadataSchemaRows = metadataSchemaRows,
            MetadataDisplayNames = metadataDisplayNameMap
        };
    }

    public Task<System.IO.Stream> DownloadAssetAsync(string assetId, CancellationToken cancellationToken = default)
        => _apiClient.DownloadAssetAsync(assetId, cancellationToken);

    private async Task WalkFolderAsync(
        string folderId,
        IDictionary<string, WebDamFolderDto> folders,
        ICollection<WebDamAssetDto> assets,
        CancellationToken cancellationToken)
    {
        var offset = 0;

        while (true)
        {
            var page = await _apiClient.GetFolderContentsAsync(folderId, offset, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (page.Items.Count == 0)
            {
                return;
            }

            var discoveredChildFolders = new List<string>();

            foreach (var item in page.Items)
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var type = TryGetProperty(item, "type");

                if (string.Equals(type, "folder", StringComparison.OrdinalIgnoreCase))
                {
                    var folder = JsonSerializer.Deserialize<WebDamFolderDto>(item.GetRawText());
                    if (folder?.Id is not null)
                    {
                        folder.Parent ??= folderId;
                        folders[folder.Id] = folder;
                        discoveredChildFolders.Add(folder.Id);
                    }
                }
                else if (string.Equals(type, "asset", StringComparison.OrdinalIgnoreCase))
                {
                    var asset = JsonSerializer.Deserialize<WebDamAssetDto>(item.GetRawText());
                    if (asset is not null)
                    {
                        assets.Add(asset);
                    }
                }
            }

            foreach (var childFolderId in discoveredChildFolders)
            {
                await WalkFolderAsync(childFolderId, folders, assets, cancellationToken).ConfigureAwait(false);
            }

            offset += page.Items.Count;

            if (offset >= page.TotalCount)
            {
                break;
            }
        }
    }

    private static IReadOnlyDictionary<string, string> BuildFolderPaths(
        IReadOnlyDictionary<string, WebDamFolderDto> folders)
    {
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string Build(string folderId)
        {
            if (cache.TryGetValue(folderId, out var existing))
            {
                return existing;
            }

            if (!folders.TryGetValue(folderId, out var folder))
            {
                return "/";
            }

            if (string.IsNullOrWhiteSpace(folder.Parent) || folder.Parent == "0")
            {
                var rootPath = "/" + (folder.Name ?? folderId);
                cache[folderId] = rootPath;
                return rootPath;
            }

            var parentPath = Build(folder.Parent);
            var path = $"{parentPath}/{folder.Name ?? folderId}";
            cache[folderId] = path;
            return path;
        }

        foreach (var folderId in folders.Keys)
        {
            Build(folderId);
        }

        return cache;
    }

    private static string GetFolderPath(string? folderId, IReadOnlyDictionary<string, string> folderPaths)
    {
        if (string.IsNullOrWhiteSpace(folderId))
        {
            return "/";
        }

        return folderPaths.TryGetValue(folderId, out var path)
            ? path
            : "/";
    }

    private static IEnumerable<List<string>> Chunk(IEnumerable<string> source, int size)
    {
        var bucket = new List<string>(size);

        foreach (var item in source)
        {
            bucket.Add(item);
            if (bucket.Count == size)
            {
                yield return bucket;
                bucket = new List<string>(size);
            }
        }

        if (bucket.Count > 0)
        {
            yield return bucket;
        }
    }

    private static string? TryGetProperty(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return prop.Value.ValueKind == JsonValueKind.String
                    ? prop.Value.GetString()
                    : prop.Value.GetRawText();
            }
        }

        return null;
    }

    private static HashSet<string> BuildMultiValueFieldSet(
        IReadOnlyCollection<WebDamXmpSchemaFieldDto> xmpSchema)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in xmpSchema)
        {
            if (string.IsNullOrWhiteSpace(field.Field))
            {
                continue;
            }

            if (IsMultiValueSchemaField(field))
            {
                result.Add(field.Field!);
            }
        }

        return result;
    }

    private static bool IsMultiValueSchemaField(WebDamXmpSchemaFieldDto field)
    {
        var type = field.Type?.Trim().ToLowerInvariant() ?? string.Empty;
        var fieldName = field.Field?.Trim().ToLowerInvariant() ?? string.Empty;
        var label = field.Label?.Trim().ToLowerInvariant() ?? string.Empty;
        var name = field.Name?.Trim().ToLowerInvariant() ?? string.Empty;

        if (fieldName == "keyword")
        {
            return true;
        }

        if (type is "multiselect" or "multichoice" or "checkbox" or "list" or "array" or "bag" or "seq")
        {
            return true;
        }

        if (fieldName.Contains("keyword") || fieldName.Contains("subject") || fieldName.Contains("tag"))
        {
            return true;
        }

        if (label.Contains("keyword") || label.Contains("tag") || name.Contains("keyword") || name.Contains("tag"))
        {
            return true;
        }

        return false;
    }

    private static Dictionary<string, string> NormalizeMetadataForExport(
        IReadOnlyDictionary<string, string> metadata,
        ISet<string> multiValueFields)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in metadata)
        {
            var normalized = multiValueFields.Contains(kvp.Key)
            ? NormalizeJoinedMultiValueString(NormalizeMultiValueField(kvp.Value))
            : NormalizeSingleValueField(kvp.Value);

            result[kvp.Key] = normalized;
        }

        return result;
    }

    private static string NormalizeJoinedMultiValueString(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        var values = rawValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values.Count == 0
            ? string.Empty
            : string.Join("; ", values);
    }

    private static string NormalizeSingleValueField(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        var trimmed = rawValue.Trim();

        // If a single-value field accidentally arrives as a JSON array with one value,
        // flatten it safely.
        if (TryParseJsonArray(trimmed, out var arrayValues))
        {
            return arrayValues.Count == 0
                ? string.Empty
                : string.Join("; ", arrayValues);
        }

        return trimmed;
    }

    private static string NormalizeMultiValueField(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        var trimmed = rawValue.Trim();

        // 1. Proper JSON array: ["dog","cat"]
        if (TryParseJsonArray(trimmed, out var jsonArrayValues))
        {
            return string.Join("; ", jsonArrayValues);
        }

        // 2. JSON object containing values-ish collections
        if (TryParseJsonObjectValues(trimmed, out var objectValues))
        {
            return string.Join("; ", objectValues);
        }

        // 3. Already-delimited string
        var splitValues = SplitPossibleMultiValueString(trimmed);
        if (splitValues.Count > 1)
        {
            return string.Join("; ", splitValues);
        }

        return trimmed;
    }

    private static bool TryParseJsonArray(string rawValue, out List<string> values)
    {
        values = new List<string>();

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawValue);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var value = JsonElementToExportString(item);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }

            values = values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseJsonObjectValues(string rawValue, out List<string> values)
    {
        values = new List<string>();

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawValue);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        var value = JsonElementToExportString(item);
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            values.Add(value);
                        }
                    }
                }
            }

            values = values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return values.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static List<string> SplitPossibleMultiValueString(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new List<string>();
        }

        var values = rawValue
            .Split(new[] { ",", ";", "|" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values.Count > 0
            ? values
            : new List<string> { rawValue.Trim() };
    }

    private static string JsonElementToExportString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Object => element.GetRawText(),
            JsonValueKind.Array => string.Join("; ", element.EnumerateArray().Select(JsonElementToExportString)),
            _ => element.ToString()
        };
    }
}
