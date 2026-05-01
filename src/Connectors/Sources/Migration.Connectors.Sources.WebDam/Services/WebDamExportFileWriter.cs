using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using Migration.Connectors.Sources.WebDam.Models;

namespace Migration.Connectors.Sources.WebDam.Services;

public static class WebDamExportFileWriter
{
    private static readonly string[] AssetColumns =
    [
        "AssetId",
        "FileName",
        "Name",
        "SizeBytes",
        "FileType",
        "FolderId",
        "FolderPath"
    ];

    public static byte[] WriteManifestCsv(WebDamExportResult export)
    {
        var metadataColumns = BuildMetadataColumns(export);
        var builder = new StringBuilder();

        builder.AppendLine(string.Join(",", AssetColumns.Concat(metadataColumns.Select(x => x.Header)).Select(Escape)));

        foreach (var asset in export.Assets)
        {
            var metadata = export.MetadataRows.FirstOrDefault(x => string.Equals(x.AssetId, asset.AssetId, StringComparison.OrdinalIgnoreCase))
                ?.Metadata
                ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            var values = new List<string?>
            {
                asset.AssetId,
                asset.FileName,
                asset.Name,
                asset.SizeBytes?.ToString(CultureInfo.InvariantCulture),
                asset.FileType,
                asset.FolderId,
                asset.FolderPath
            };

            values.AddRange(metadataColumns.Select(column =>
                metadata.TryGetValue(column.Field, out var value) ? value : null));

            builder.AppendLine(string.Join(",", values.Select(Escape)));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public static byte[] WriteWorkbook(WebDamExportResult export)
    {
        using var memory = new MemoryStream();

        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", ContentTypesXml());
            AddEntry(archive, "_rels/.rels", RootRelationshipsXml());
            AddEntry(archive, "xl/workbook.xml", WorkbookXml());
            AddEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml());
            AddEntry(archive, "xl/worksheets/sheet1.xml", WorksheetXml(BuildAssetsSheet(export)));
            AddEntry(archive, "xl/worksheets/sheet2.xml", WorksheetXml(BuildMetadataSheet(export)));
            AddEntry(archive, "xl/worksheets/sheet3.xml", WorksheetXml(BuildMetadataSchemaSheet(export)));
        }

        return memory.ToArray();
    }

    private static IReadOnlyList<(string Field, string Header)> BuildMetadataColumns(WebDamExportResult export)
    {
        return export.MetadataSchemaRows
            .Where(x => !string.IsNullOrWhiteSpace(x.Field))
            .Select(x =>
            {
                var label = export.MetadataDisplayNames.TryGetValue(x.Field, out var displayName)
                    ? displayName
                    : x.Label ?? x.Name ?? x.Field;

                return (Field: x.Field, Header: $"{label} ({x.Field})");
            })
            .OrderBy(x => x.Header, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<IReadOnlyList<string?>> BuildAssetsSheet(WebDamExportResult export)
    {
        var rows = new List<IReadOnlyList<string?>>();

        rows.Add(AssetColumns);

        foreach (var asset in export.Assets)
        {
            rows.Add([
                asset.AssetId,
                asset.FileName,
                asset.Name,
                asset.SizeBytes?.ToString(CultureInfo.InvariantCulture),
                asset.FileType,
                asset.FolderId,
                asset.FolderPath
            ]);
        }

        return rows;
    }

    private static IReadOnlyList<IReadOnlyList<string?>> BuildMetadataSheet(WebDamExportResult export)
    {
        var metadataColumns = BuildMetadataColumns(export);
        var rows = new List<IReadOnlyList<string?>>();

        rows.Add(["AssetId", .. metadataColumns.Select(x => x.Header)]);

        foreach (var asset in export.Assets)
        {
            var metadata = export.MetadataRows.FirstOrDefault(x => string.Equals(x.AssetId, asset.AssetId, StringComparison.OrdinalIgnoreCase))
                ?.Metadata
                ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            var values = new List<string?> { asset.AssetId };
            values.AddRange(metadataColumns.Select(column =>
                metadata.TryGetValue(column.Field, out var value) ? value : null));

            rows.Add(values);
        }

        return rows;
    }

    private static IReadOnlyList<IReadOnlyList<string?>> BuildMetadataSchemaSheet(WebDamExportResult export)
    {
        var rows = new List<IReadOnlyList<string?>>
        {
            new string?[]
            {
                "Field",
                "Name",
                "Label",
                "Status",
                "Searchable",
                "Position",
                "Type",
                "PossibleValues"
            }
        };

        foreach (var item in export.MetadataSchemaRows)
        {
            rows.Add(new string?[]
            {
                item.Field,
                item.Name,
                item.Label,
                item.Status,
                item.Searchable,
                item.Position,
                item.Type,
                item.PossibleValues
            });
        }

        return rows;
    }

    private static string WorksheetXml(IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        var builder = new StringBuilder();

        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            builder.Append(CultureInfo.InvariantCulture, $"<row r=\"{rowIndex + 1}\">");

            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                var reference = $"{ColumnName(columnIndex + 1)}{rowIndex + 1}";
                builder.Append(CultureInfo.InvariantCulture, $"<c r=\"{reference}\" t=\"inlineStr\"><is><t>{Xml(row[columnIndex])}</t></is></c>");
            }

            builder.Append("</row>");
        }

        builder.Append("</sheetData></worksheet>");

        return builder.ToString();
    }

    private static string ColumnName(int index)
    {
        var name = string.Empty;

        while (index > 0)
        {
            var modulo = (index - 1) % 26;
            name = Convert.ToChar('A' + modulo) + name;
            index = (index - modulo) / 26;
        }

        return name;
    }

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        var mustQuote = text.Contains(',') || text.Contains('"') || text.Contains('\r') || text.Contains('\n');

        if (text.Contains('"'))
        {
            text = text.Replace("\"", "\"\"");
        }

        return mustQuote ? $"\"{text}\"" : text;
    }

    private static string Xml(string? value)
        => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);

        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(content);
    }

    private static string ContentTypesXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""" +
        """<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""" +
        """<Default Extension="xml" ContentType="application/xml"/>""" +
        """<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""" +
        """<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""" +
        """<Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""" +
        """<Override PartName="/xl/worksheets/sheet3.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""" +
        """</Types>""";

    private static string RootRelationshipsXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
        """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>""" +
        """</Relationships>""";

    private static string WorkbookXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" """ +
        """xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">""" +
        """<sheets>""" +
        """<sheet name="Assets" sheetId="1" r:id="rId1"/>""" +
        """<sheet name="Metadata" sheetId="2" r:id="rId2"/>""" +
        """<sheet name="Metadata Schema" sheetId="3" r:id="rId3"/>""" +
        """</sheets></workbook>""";

    private static string WorkbookRelationshipsXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
        """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>""" +
        """<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>""" +
        """<Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet3.xml"/>""" +
        """</Relationships>""";
}