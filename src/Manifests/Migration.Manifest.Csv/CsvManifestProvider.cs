using System.Text;
using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Manifest.Csv;

public sealed class CsvManifestProvider : IManifestProvider
{
    private readonly IArtifactContentResolver? _artifactContentResolver;

    public CsvManifestProvider(IArtifactContentResolver? artifactContentResolver = null)
    {
        _artifactContentResolver = artifactContentResolver;
    }

    public string Type => "Csv";

    public async Task<IReadOnlyList<ManifestRow>> ReadAsync(
        MigrationJobDefinition job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (string.IsNullOrWhiteSpace(job.ManifestPath))
        {
            return Array.Empty<ManifestRow>();
        }

        if (_artifactContentResolver is not null && _artifactContentResolver.IsArtifactReference(job.ManifestPath))
        {
            await using var artifact = await _artifactContentResolver
                .OpenReadAsync(job.ManifestPath, cancellationToken)
                .ConfigureAwait(false);

            using var reader = new StreamReader(
                artifact.Content,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: false);

            return await ReadRowsAsync(reader, cancellationToken).ConfigureAwait(false);
        }

        await using var file = File.OpenRead(job.ManifestPath);
        using var fileReader = new StreamReader(file, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await ReadRowsAsync(fileReader, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<ManifestRow>> ReadRowsAsync(
        TextReader reader,
        CancellationToken cancellationToken)
    {
        var headerLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return Array.Empty<ManifestRow>();
        }

        var headers = SplitCsvLine(headerLine);
        var rows = new List<ManifestRow>();
        var rowNumber = 1;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = SplitCsvLine(line);
            var columns = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
            {
                columns[headers[c]] = c < values.Count ? values[c] : null;
            }

            rows.Add(new ManifestRow
            {
                RowId = GetFirst(columns, "RowId", "row_id", "rowId")
                    ?? GetFirst(columns, "webdam_id", "webdamId", "WebDamId", "SourceAssetId", "AssetId", "Id")
                    ?? rowNumber.ToString(),
                SourceAssetId = GetFirst(columns, "SourceAssetId", "source_asset_id", "sourceAssetId", "webdam_id", "webdamId", "WebDamId", "AssetId", "asset_id", "Id", "id"),
                SourcePath = GetFirst(columns, "SourcePath", "source_path", "sourcePath", "Path", "path", "FilePath", "file_path", "filePath", "SourceUri", "source_uri", "sourceUri", "DownloadUrl", "download_url", "downloadUrl", "Url", "url"),
                Columns = columns
            });
        }

        return rows;
    }

    private static string? GetFirst(Dictionary<string, string?> columns, params string[] names)
        => names.Select(n => columns.TryGetValue(n, out var v) ? v : null)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}
