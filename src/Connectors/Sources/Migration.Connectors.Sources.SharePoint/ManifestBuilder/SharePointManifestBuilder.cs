using System.Text;
using Microsoft.Extensions.Options;
using Migration.Connectors.Sources.SharePoint.Configuration;
using Migration.Connectors.Sources.SharePoint.Graph;
using Migration.Connectors.Sources.SharePoint.Rclone;
using Migration.Connectors.Sources.SharePoint.Services;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.SharePoint.ManifestBuilder;

public sealed class SharePointManifestBuilder : ISharePointManifestBuilder
{
    private readonly SharePointSourceOptions _options;
    private readonly RcloneSharePointSourceService _rclone;
    private readonly GraphSharePointSourceService _graph;

    public SharePointManifestBuilder(
        IOptions<SharePointSourceOptions> options,
        RcloneSharePointSourceService rclone,
        GraphSharePointSourceService graph)
    {
        _options = options.Value;
        _rclone = rclone;
        _graph = graph;
    }

    public async Task<IReadOnlyList<ManifestRow>> BuildAsync(MigrationJobDefinition job, CancellationToken cancellationToken = default)
    {
        var mode = ResolveMode(job);
        return mode.Equals("Graph", StringComparison.OrdinalIgnoreCase)
            ? await BuildGraphManifestAsync(job, cancellationToken).ConfigureAwait(false)
            : await BuildRcloneManifestAsync(job, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteCsvAsync(MigrationJobDefinition job, string outputPath, CancellationToken cancellationToken = default)
    {
        var rows = await BuildAsync(job, cancellationToken).ConfigureAwait(false);
        var allColumns = rows.SelectMany(r => r.Columns.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var headers = new List<string> { "RowId", "SourceAssetId", "SourcePath" };
        headers.AddRange(allColumns.Where(c => !headers.Contains(c, StringComparer.OrdinalIgnoreCase)));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".");
        await using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 128, true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteLineAsync(string.Join(',', headers.Select(Csv))).ConfigureAwait(false);

        foreach (var row in rows)
        {
            var values = headers.Select(h => h switch
            {
                "RowId" => row.RowId,
                "SourceAssetId" => row.SourceAssetId ?? string.Empty,
                "SourcePath" => row.SourcePath ?? string.Empty,
                _ => row.Columns.TryGetValue(h, out var value) ? value : string.Empty
            });
            await writer.WriteLineAsync(string.Join(',', values.Select(Csv))).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<ManifestRow>> BuildRcloneManifestAsync(MigrationJobDefinition job, CancellationToken cancellationToken)
    {
        var files = await _rclone.ListFilesAsync(job, _options, cancellationToken).ConfigureAwait(false);
        return files.Select((path, index) =>
        {
            var metadata = SharePointPathUtilities.BuildPathMetadata(path);
            metadata["_manifest_source"] = "SharePointRclone";
            return new ManifestRow
            {
                RowId = (index + 1).ToString(),
                SourceAssetId = path,
                SourcePath = path,
                Columns = metadata
            };
        }).ToList();
    }

    private async Task<IReadOnlyList<ManifestRow>> BuildGraphManifestAsync(MigrationJobDefinition job, CancellationToken cancellationToken)
    {
        var files = await _graph.ListFilesAsync(job, _options, cancellationToken).ConfigureAwait(false);
        return files.Select((item, index) =>
        {
            var path = item.RelativePath ?? item.Name ?? item.Id ?? index.ToString();
            var metadata = SharePointPathUtilities.BuildPathMetadata(path);
            metadata["_manifest_source"] = "SharePointGraph";
            if (_options.Manifest.IncludeGraphMetadata)
            {
                Add(metadata, "drive_item_id", item.Id);
                Add(metadata, "web_url", item.WebUrl);
                Add(metadata, "etag", item.ETag);
                Add(metadata, "created_datetime", item.CreatedDateTime);
                Add(metadata, "modified_datetime", item.LastModifiedDateTime);
                Add(metadata, "created_by", item.CreatedBy?.User?.DisplayName);
                Add(metadata, "modified_by", item.LastModifiedBy?.User?.DisplayName);
                if (item.Size.HasValue) metadata["size"] = item.Size.Value.ToString();
            }

            return new ManifestRow
            {
                RowId = (index + 1).ToString(),
                SourceAssetId = item.Id ?? path,
                SourcePath = path,
                Columns = metadata
            };
        }).ToList();
    }

    private string ResolveMode(MigrationJobDefinition job)
    {
        if (job.Settings.TryGetValue("SharePointMode", out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        if (job.Settings.TryGetValue("ManifestSharePointMode", out value) && !string.IsNullOrWhiteSpace(value)) return value;
        return _options.Mode;
    }

    private static void Add(IDictionary<string, string> values, string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) values[key] = value; }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
