using Migration.Application.Abstractions;
using Migration.Domain.Models;
using OfficeOpenXml;

namespace Migration.Manifest.Excel;

/// <summary>
/// Excel manifest provider for the generic migration runner.
///
/// Supports .xlsx/.xlsm manifests with the same logical column names as the CSV provider.
/// The first row is treated as the header row by default.
/// </summary>
public sealed class ExcelManifestProvider : IManifestProvider
{
    public string Type => "Excel";

    public async Task<IReadOnlyList<ManifestRow>> ReadAsync(
        MigrationJobDefinition job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (string.IsNullOrWhiteSpace(job.ManifestPath))
        {
            return Array.Empty<ManifestRow>();
        }

        if (!File.Exists(job.ManifestPath))
        {
            throw new FileNotFoundException($"Excel manifest file was not found: {job.ManifestPath}", job.ManifestPath);
        }

        var extension = Path.GetExtension(job.ManifestPath);
        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return await new Migration.Manifest.Csv.CsvManifestProvider()
                .ReadAsync(job, cancellationToken)
                .ConfigureAwait(false);
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage(new FileInfo(job.ManifestPath));
        var worksheet = ResolveWorksheet(package, job);

        if (worksheet.Dimension is null)
        {
            return Array.Empty<ManifestRow>();
        }

        var headerRowNumber = GetIntSetting(job, 1, "HeaderRow", "ExcelHeaderRow", "WorksheetHeaderRow");
        var firstDataRowNumber = GetIntSetting(job, headerRowNumber + 1, "FirstDataRow", "ExcelFirstDataRow", "WorksheetFirstDataRow");

        var startColumn = worksheet.Dimension.Start.Column;
        var endColumn = worksheet.Dimension.End.Column;
        var endRow = worksheet.Dimension.End.Row;

        var headers = new List<(int Column, string Name)>();

        for (var column = startColumn; column <= endColumn; column++)
        {
            var header = worksheet.Cells[headerRowNumber, column].Text?.Trim();

            if (!string.IsNullOrWhiteSpace(header))
            {
                headers.Add((column, header));
            }
        }

        if (headers.Count == 0)
        {
            throw new InvalidOperationException(
                $"Excel manifest worksheet '{worksheet.Name}' did not contain any headers on row {headerRowNumber}.");
        }

        var rows = new List<ManifestRow>();

        for (var rowNumber = firstDataRowNumber; rowNumber <= endRow; rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var columns = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var hasAnyValue = false;

            foreach (var header in headers)
            {
                var value = ReadCellAsString(worksheet, rowNumber, header.Column);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    hasAnyValue = true;
                }

                columns[header.Name] = value;
            }

            if (!hasAnyValue)
            {
                continue;
            }

            rows.Add(new ManifestRow
            {
                RowId = GetFirst(columns, "RowId", "row_id", "rowId")
                        ?? GetFirst(columns, "webdam_id", "webdamId", "WebDamId", "SourceAssetId", "AssetId", "Id")
                        ?? rowNumber.ToString(),
                SourceAssetId = GetFirst(
                    columns,
                    "SourceAssetId",
                    "source_asset_id",
                    "sourceAssetId",
                    "webdam_id",
                    "webdamId",
                    "WebDamId",
                    "AssetId",
                    "asset_id",
                    "Id",
                    "id"),
                SourcePath = GetFirst(
                    columns,
                    "SourcePath",
                    "source_path",
                    "sourcePath",
                    "Path",
                    "path",
                    "FilePath",
                    "file_path",
                    "filePath",
                    "SourceUri",
                    "source_uri",
                    "sourceUri",
                    "DownloadUrl",
                    "download_url",
                    "downloadUrl",
                    "Url",
                    "url"),
                Columns = columns
            });
        }

        return rows;
    }

    private static ExcelWorksheet ResolveWorksheet(ExcelPackage package, MigrationJobDefinition job)
    {
        var worksheetName = GetSetting(job, "WorksheetName", "ExcelWorksheetName", "SheetName", "ExcelSheetName");

        if (!string.IsNullOrWhiteSpace(worksheetName))
        {
            var namedWorksheet = package.Workbook.Worksheets
                .FirstOrDefault(x => x.Name.Equals(worksheetName, StringComparison.OrdinalIgnoreCase));

            if (namedWorksheet is null)
            {
                throw new InvalidOperationException(
                    $"Excel manifest worksheet '{worksheetName}' was not found. Available worksheets: {string.Join(", ", package.Workbook.Worksheets.Select(x => x.Name))}");
            }

            return namedWorksheet;
        }

        var firstWorksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (firstWorksheet is null)
        {
            throw new InvalidOperationException("Excel manifest did not contain any worksheets.");
        }

        return firstWorksheet;
    }

    private static string? ReadCellAsString(ExcelWorksheet worksheet, int row, int column)
    {
        var cell = worksheet.Cells[row, column];

        if (cell.Value is null)
        {
            return null;
        }

        if (cell.Value is DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        return cell.Text?.Trim();
    }

    private static int GetIntSetting(MigrationJobDefinition job, int defaultValue, params string[] names)
    {
        var setting = GetSetting(job, names);

        return int.TryParse(setting, out var value) && value > 0
            ? value
            : defaultValue;
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

    private static string? GetFirst(Dictionary<string, string?> columns, params string[] names)
        => names
            .Select(n => columns.TryGetValue(n, out var v) ? v : null)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
