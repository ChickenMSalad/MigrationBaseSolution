using Migration.Application.Taxonomy;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace Migration.Infrastructure.Taxonomy;

public sealed class TaxonomyExcelWriter : ITaxonomyExcelWriter
{
    public async Task WriteAsync(TaxonomyWorkbook workbook, string outputPath, CancellationToken cancellationToken)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var file = new FileInfo(outputPath);
        file.Directory?.Create();

        using var package = new ExcelPackage();
        AddFieldsSheet(package, workbook.Fields);
        AddOptionsSheet(package, workbook.Options);

        if (!string.IsNullOrWhiteSpace(workbook.RawJson))
        {
            AddRawSheet(package, workbook.TargetType, workbook.RawJson!);
        }

        await package.SaveAsAsync(file, cancellationToken).ConfigureAwait(false);
    }

    private static void AddFieldsSheet(ExcelPackage package, IReadOnlyList<TaxonomyField> fields)
    {
        var ws = package.Workbook.Worksheets.Add("Fields");
        var headers = new[] { "TargetType", "Id", "Name", "Label", "Type", "Required", "Searchable", "MultiValue", "GroupName", "Status", "SortOrder" };
        AddHeaders(ws, headers);

        for (var i = 0; i < fields.Count; i++)
        {
            var r = i + 2;
            var f = fields[i];
            ws.Cells[r, 1].Value = f.TargetType;
            ws.Cells[r, 2].Value = f.Id;
            ws.Cells[r, 3].Value = f.Name;
            ws.Cells[r, 4].Value = f.Label;
            ws.Cells[r, 5].Value = f.Type;
            ws.Cells[r, 6].Value = f.Required;
            ws.Cells[r, 7].Value = f.Searchable;
            ws.Cells[r, 8].Value = f.MultiValue;
            ws.Cells[r, 9].Value = f.GroupName;
            ws.Cells[r, 10].Value = f.Status;
            ws.Cells[r, 11].Value = f.SortOrder;
        }

        FormatAsTable(ws, "TaxonomyFieldsTable", fields.Count + 1, headers.Length);
    }

    private static void AddOptionsSheet(ExcelPackage package, IReadOnlyList<TaxonomyOption> options)
    {
        var ws = package.Workbook.Worksheets.Add("Options");
        var headers = new[] { "TargetType", "FieldId", "FieldName", "Id", "Name", "Label", "Selectable", "SortOrder", "ParentOptionId", "LinkedOptionIds" };
        AddHeaders(ws, headers);

        for (var i = 0; i < options.Count; i++)
        {
            var r = i + 2;
            var o = options[i];
            ws.Cells[r, 1].Value = o.TargetType;
            ws.Cells[r, 2].Value = o.FieldId;
            ws.Cells[r, 3].Value = o.FieldName;
            ws.Cells[r, 4].Value = o.Id;
            ws.Cells[r, 5].Value = o.Name;
            ws.Cells[r, 6].Value = o.Label;
            ws.Cells[r, 7].Value = o.Selectable;
            ws.Cells[r, 8].Value = o.SortOrder;
            ws.Cells[r, 9].Value = o.ParentOptionId;
            ws.Cells[r, 10].Value = o.LinkedOptionIds;
        }

        FormatAsTable(ws, "TaxonomyOptionsTable", options.Count + 1, headers.Length);
    }

    private static void AddRawSheet(ExcelPackage package, string targetType, string rawJson)
    {
        var name = string.IsNullOrWhiteSpace(targetType) ? "Raw" : $"{targetType}_Raw";
        if (name.Length > 31) name = name[..31];

        var ws = package.Workbook.Worksheets.Add(name);
        ws.Cells[1, 1].Value = "RawJson";
        ws.Cells[2, 1].Value = rawJson;
        ws.Cells[2, 1].Style.WrapText = false;
        ws.Column(1).Width = 120;
        ws.View.FreezePanes(2, 1);
    }

    private static void AddHeaders(ExcelWorksheet ws, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
        }
    }

    private static void FormatAsTable(ExcelWorksheet ws, string tableName, int rows, int cols)
    {
        var safeRows = Math.Max(rows, 2);
        var range = ws.Cells[1, 1, safeRows, cols];
        var table = ws.Tables.Add(range, tableName);
        table.TableStyle = TableStyles.Medium2;
        ws.View.FreezePanes(2, 1);
        ws.Cells[ws.Dimension.Address].AutoFitColumns();
    }
}
