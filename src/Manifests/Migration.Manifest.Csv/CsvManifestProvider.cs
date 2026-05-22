using System.Text;
using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Manifest.Csv;

public sealed class CsvManifestProvider : IManifestProvider
{
    public string Type => "Csv";

    public async Task<IReadOnlyList<ManifestRow>> ReadAsync(MigrationJobDefinition job, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(job.ManifestPath)) return Array.Empty<ManifestRow>();
        var lines = await File.ReadAllLinesAsync(job.ManifestPath, cancellationToken);
        if (lines.Length == 0) return Array.Empty<ManifestRow>();
        var headers = SplitCsvLine(lines[0]);
        var rows = new List<ManifestRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var values = SplitCsvLine(lines[i]);
            var columns = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++) columns[headers[c]] = c < values.Count ? values[c] : null;
            rows.Add(new ManifestRow
            {
                RowId = GetFirst(columns, "RowId", "row_id", "rowId") ?? GetFirst(columns, "webdam_id", "webdamId", "WebDamId", "SourceAssetId", "AssetId", "Id") ?? i.ToString(),
                SourceAssetId = GetFirst(columns, "SourceAssetId", "source_asset_id", "sourceAssetId", "webdam_id", "webdamId", "WebDamId", "AssetId", "asset_id", "Id", "id"),
                SourcePath = GetFirst(columns, "SourcePath", "source_path", "sourcePath", "Path", "path", "FilePath", "file_path", "filePath", "SourceUri", "source_uri", "sourceUri", "DownloadUrl", "download_url", "downloadUrl", "Url", "url"),
                Columns = columns
            });
        }
        return rows;
    }

    private static string? GetFirst(Dictionary<string, string?> columns, params string[] names)
        => names.Select(n => columns.TryGetValue(n, out var v) ? v : null).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>(); var sb = new StringBuilder(); var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes) { result.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(ch);
        }
        result.Add(sb.ToString()); return result;
    }
}
