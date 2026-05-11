using Migration.Connectors.Sources.Aem.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Files
{
    public static class ExcelSplitter
    {
        /// <summary>
        /// Reads an Excel file, filters rows, groups by the Xth path part from column C,
        /// and writes one Excel file per distinct path part.
        /// 
        /// Header: Id,Name,Path,MimeType,SizeBytes,Created,LastModified
        /// Column A: Id
        /// Column C: Path
        /// Column E: SizeBytes
        /// </summary>
        public static void SplitByPathPart(string inputExcelPath, string outputFolder, int folderPart, bool returnOnlyFromFolderPart)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // EPPlus licensing (set this once in your app startup)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // or LicenseContext.Commercial

            var filteredRows = new List<(AemAsset Asset, string PathPart)>();

            using (var package = new ExcelPackage(new FileInfo(inputExcelPath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // first worksheet (index is 0-based for the collection)

                if (worksheet.Dimension == null)
                    return; // empty sheet

                int startRow = worksheet.Dimension.Start.Row + 1;  // skip header row
                int endRow = worksheet.Dimension.End.Row;

                for (int row = startRow; row <= endRow; row++)
                {
                    var id = worksheet.Cells[row, 1].Text; // A
                    var name = worksheet.Cells[row, 2].Text; // B
                    var path = worksheet.Cells[row, 3].Text; // C
                    var mimeType = worksheet.Cells[row, 4].Text; // D
                    long size = ToLongSafe(worksheet.Cells[row, 5]); // E
                    var created = worksheet.Cells[row, 6].Text;   //ToDateTimeNullable(worksheet.Cells[row, 6]); // F
                    var modified = worksheet.Cells[row, 7].Text;  //ToDateTimeNullable(worksheet.Cells[row, 7]); // G

                    // Ignore rows where Id is null/empty
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    // Ignore rows where SizeBytes == 0
                    if (size == 0)
                        continue;

                    var pathPart = GetXthPathPart(path, folderPart, returnOnlyFromFolderPart);
                    if (string.IsNullOrWhiteSpace(pathPart))
                        continue;

                    var asset = new AemAsset
                    {
                        Id = id,
                        Name = name,
                        Path = path,
                        MimeType = mimeType,
                        SizeBytes = size,
                        Created = created,
                        LastModified = modified
                    };

                    filteredRows.Add((asset, pathPart));
                }
            }

            // Group by the path part (Xth segment) and create a file per group

            var groups = filteredRows.GroupBy(x => x.PathPart);

            foreach (var group in groups)
            {
                string pathPart = group.Key;
                string safeFileName = MakeSafeFileName(pathPart) + ".xlsx";
                string outputPath = Path.Combine(outputFolder, safeFileName);

                using (var outPackage = new ExcelPackage())
                {
                    var outWs = outPackage.Workbook.Worksheets.Add("Assets");

                    // Header row
                    outWs.Cells[1, 1].Value = "Id";
                    outWs.Cells[1, 2].Value = "Name";
                    outWs.Cells[1, 3].Value = "Path";
                    outWs.Cells[1, 4].Value = "MimeType";
                    outWs.Cells[1, 5].Value = "SizeBytes";
                    outWs.Cells[1, 6].Value = "Created";
                    outWs.Cells[1, 7].Value = "LastModified";

                    int rowIndex = 2;

                    foreach (var item in group)
                    {
                        var a = item.Asset;

                        outWs.Cells[rowIndex, 1].Value = a.Id;
                        outWs.Cells[rowIndex, 2].Value = a.Name;
                        outWs.Cells[rowIndex, 3].Value = a.Path;
                        outWs.Cells[rowIndex, 4].Value = a.MimeType;
                        outWs.Cells[rowIndex, 5].Value = a.SizeBytes;

                        outWs.Cells[rowIndex, 6].Value = a.Created;

                        outWs.Cells[rowIndex, 7].Value = a.LastModified;

                        rowIndex++;
                    }

                    outPackage.SaveAs(new FileInfo(outputPath));
                }
            }
        }

        // Ex:  Extracts 5th segment from a path like:
        // "/content/dam/ashley-furniture/3rdparty/Bedgear/M60000259-3PIS" => "Bedgear"
        private static string GetXthPathPart(string path, int folderPart, bool returnOnlyFromFolderPart)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (returnOnlyFromFolderPart)
            {
                if(parts.Length > folderPart+1)
                {
                    return null;
                }
                else
                {
                    return parts[folderPart-2];
                }
            }

            // [0]=content, [1]=dam, [2]=ashley-furniture, [3]=3rdparty, [4]=Bedgear, [5]=M60000259-3PIS
            return parts.Length >= folderPart ? parts[folderPart-1] : null;


        }

        private static long ToLongSafe(ExcelRange cell)
        {
            // EPPlus often stores numbers as double
            try
            {
                var val = cell.Value;
                if (val == null)
                    return 0;

                if (val is double d)
                    return (long)d;

                if (val is int i)
                    return i;

                if (long.TryParse(val.ToString(), out var l))
                    return l;
            }
            catch
            {
                // ignore and fall through
            }
            return 0;
        }

        private static DateTime? ToDateTimeNullable(ExcelRange cell)
        {
            var val = cell.Value;
            if (val == null)
                return null;

            if (val is DateTime dt)
                return dt;

            if (DateTime.TryParse(val.ToString(), out var parsed))
                return parsed;

            return null;
        }

        private static string MakeSafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        }

        public static void SplitExcelFile(string sourcePath, string outputDirectory, string prefix, string taxonomy, int rowsPerFile = 500)
        {
            // EPPlus licensing (set this once in your app startup)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // or LicenseContext.Commercial
            using (var package = new ExcelPackage(new FileInfo(sourcePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Assuming first worksheet
                int totalRows = worksheet.Dimension.End.Row;
                int totalColumns = worksheet.Dimension.End.Column;

                // Read header row
                var header = new object[totalColumns];
                for (int col = 1; col <= totalColumns; col++)
                {
                    header[col - 1] = worksheet.Cells[1, col].Value;
                }

                int fileIndex = 1;
                for (int startRow = 2; startRow <= totalRows; startRow += rowsPerFile)
                {
                    string newFile = Path.Combine(outputDirectory, $"{prefix}_{taxonomy}{fileIndex}.xlsx");
                    using (var newPackage = new ExcelPackage())
                    {
                        var newWorksheet = newPackage.Workbook.Worksheets.Add("Sheet1");

                        // Copy header
                        for (int col = 1; col <= totalColumns; col++)
                        {
                            newWorksheet.Cells[1, col].Value = header[col - 1];
                        }

                        // Copy rows
                        int endRow = Math.Min(startRow + rowsPerFile - 1, totalRows);
                        int newRow = 2;
                        for (int row = startRow; row <= endRow; row++, newRow++)
                        {
                            for (int col = 1; col <= totalColumns; col++)
                            {
                                newWorksheet.Cells[newRow, col].Value = worksheet.Cells[row, col].Value;
                            }
                        }

                        newPackage.SaveAs(new FileInfo(newFile));
                    }
                    fileIndex++;
                }
            }
        }


    }

}
