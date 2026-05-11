using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Migration.Connectors.Sources.WebDam.Models;

namespace Migration.Connectors.Sources.WebDam.Services;

public sealed class WebDamExcelExporter
{
    public void Write(Stream output, WebDamExportResult export)
    {
        if (output is null) throw new ArgumentNullException(nameof(output));
        if (export is null) throw new ArgumentNullException(nameof(export));

        using var package = new ExcelPackage();

        WriteAssetsWorksheet(package, export);
        WriteMetadataWorksheet(package, export);
        WriteMetadataSchemaWorksheet(package, export);

        package.SaveAs(output);
    }

    private static void WriteAssetsWorksheet(ExcelPackage package, WebDamExportResult export)
    {
        var worksheet = package.Workbook.Worksheets.Add("Assets");

        var headers = new[]
        {
            "Asset Id",
            "File Name",
            "Asset Name",
            "Size Bytes",
            "File Type",
            "Folder Id",
            "Folder Path"
        };

        WriteHeaderRow(worksheet, headers);

        var row = 2;
        foreach (var asset in export.Assets
                     .OrderBy(x => x.FolderPath, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(x => x.FileName, StringComparer.OrdinalIgnoreCase))
        {
            var col = 1;
            worksheet.Cells[row, col++].Value = asset.AssetId;
            worksheet.Cells[row, col++].Value = asset.FileName;
            worksheet.Cells[row, col++].Value = asset.Name ?? string.Empty;
            worksheet.Cells[row, col++].Value = asset.SizeBytes;
            worksheet.Cells[row, col++].Value = asset.FileType ?? string.Empty;
            worksheet.Cells[row, col++].Value = asset.FolderId;
            worksheet.Cells[row, col++].Value = asset.FolderPath;
            row++;
        }

        FormatWorksheetAsTable(worksheet, headers.Length, Math.Max(2, row - 1), "AssetsTable");
    }

    private static void WriteMetadataWorksheet(ExcelPackage package, WebDamExportResult export)
    {
        var worksheet = package.Workbook.Worksheets.Add("Metadata");

        // Only include ACTIVE schema fields in the metadata worksheet.
        var activeSchemaFields = export.MetadataSchemaRows
            .Where(x => string.Equals(x.Status, "active", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Field)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var headers = new List<string> { "Asset Id" };
        headers.AddRange(activeSchemaFields.Select(field => BuildMetadataHeader(field, export.MetadataDisplayNames)));

        WriteHeaderRow(worksheet, headers);

        var row = 2;
        foreach (var metadataRow in export.MetadataRows.OrderBy(x => x.AssetId, StringComparer.OrdinalIgnoreCase))
        {
            var col = 1;
            worksheet.Cells[row, col++].Value = metadataRow.AssetId;

            foreach (var field in activeSchemaFields)
            {
                metadataRow.Metadata.TryGetValue(field, out var value);
                worksheet.Cells[row, col++].Value = NormalizeCellValue(value);
            }

            row++;
        }

        FormatWorksheetAsTable(worksheet, headers.Count, Math.Max(2, row - 1), "MetadataTable");
    }

    private static string NormalizeCellValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r\n", " | ")
            .Replace("\n", " | ")
            .Replace("\r", " | ");
    }

    private static void WriteMetadataSchemaWorksheet(ExcelPackage package, WebDamExportResult export)
    {
        var worksheet = package.Workbook.Worksheets.Add("Metadata Schema");

        var headers = new[]
        {
            "Field",
            "Label",
            "Name",
            "Type",
            "Status",
            "Searchable",
            "Position",
            "Possible Values"
        };

        WriteHeaderRow(worksheet, headers);

        var row = 2;
        foreach (var schemaRow in export.MetadataSchemaRows
                     .OrderBy(x => x.Label ?? x.Name ?? x.Field, StringComparer.OrdinalIgnoreCase))
        {
            var col = 1;
            worksheet.Cells[row, col++].Value = schemaRow.Field;
            worksheet.Cells[row, col++].Value = schemaRow.Label ?? string.Empty;
            worksheet.Cells[row, col++].Value = schemaRow.Name ?? string.Empty;
            worksheet.Cells[row, col++].Value = schemaRow.Type ?? string.Empty;
            worksheet.Cells[row, col++].Value = schemaRow.Status ?? string.Empty;
            worksheet.Cells[row, col++].Value = schemaRow.Searchable ?? string.Empty;
            worksheet.Cells[row, col++].Value = schemaRow.Position ?? string.Empty;
            worksheet.Cells[row, col++].Value = schemaRow.PossibleValues ?? string.Empty;
            row++;
        }

        FormatWorksheetAsTable(worksheet, headers.Length, Math.Max(2, row - 1), "MetadataSchemaTable");
    }

    private static string BuildMetadataHeader(
        string field,
        IReadOnlyDictionary<string, string> metadataDisplayNames)
    {
        if (metadataDisplayNames.TryGetValue(field, out var displayName) &&
            !string.IsNullOrWhiteSpace(displayName) &&
            !string.Equals(displayName, field, StringComparison.OrdinalIgnoreCase))
        {
            return $"{displayName} ({field})";
        }

        return field;
    }

    private static void WriteHeaderRow(ExcelWorksheet worksheet, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
        }

        using var range = worksheet.Cells[1, 1, 1, headers.Count];
        range.Style.Font.Bold = true;
    }

    private static void FormatWorksheetAsTable(
        ExcelWorksheet worksheet,
        int columnCount,
        int rowCount,
        string tableName)
    {
        if (columnCount <= 0 || rowCount <= 0)
        {
            return;
        }

        var range = worksheet.Cells[1, 1, rowCount, columnCount];
        var table = worksheet.Tables.Add(range, tableName);
        table.TableStyle = TableStyles.Medium2;

        worksheet.View.FreezePanes(2, 1);

        if (worksheet.Dimension != null)
        {
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }
    }
}
