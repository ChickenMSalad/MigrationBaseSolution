using System.Data;

using OfficeOpenXml;

namespace Migration.Connectors.Targets.Aprimo.Files
{
    public class ExcelReader
    {
        public static List<DataTable> LoadExcelWorksheetsToDataTables(string fileName, Stream fileStream, bool hasHeader = true)
        {
            var extension = Path.GetExtension(fileName);

            if (extension != ".xlsx" && extension != ".xls")
            {
                throw new Exception("Not Valid File Type");
            }
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                package.Load(fileStream);

                var dataTables = new List<DataTable>();

                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    try
                    {
                        var dataTable = GetDataTableFromWorkSheet(worksheet, hasHeader);
                        if (dataTable == null) continue;

                        dataTables.Add(dataTable);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Error process file: {fileName} on worksheet: {worksheet.Name}, to datatable.", e);
                    }
                }

                return dataTables;
            }
        }

        public static List<DataTable> LoadExcelWorksheetsToDataTables(Stream stream, bool hasHeader = true)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(stream))
            {
                var dataTables = new List<DataTable>();

                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    try
                    {
                        var dataTable = GetDataTableFromWorkSheet(worksheet, hasHeader);
                        if (dataTable == null) continue;

                        dataTables.Add(dataTable);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Error processing worksheet: {worksheet.Name}, to datatable.", e);
                    }
                }

                return dataTables;
            }
        }

        public static List<DataTable> LoadExcelWorksheetsToDataTables(FileInfo fileInfo, bool hasHeader = true)
        {
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(fileInfo))
            {
                var dataTables = new List<DataTable>();

                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    try
                    {
                        var dataTable = GetDataTableFromWorkSheet(worksheet, hasHeader);
                        if (dataTable == null) continue;

                        dataTables.Add(dataTable);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Error processing file: {fileName} on worksheet: {worksheet.Name}, to datatable.", e);
                    }
                }

                return dataTables;
            }
        }

        private static DataTable GetDataTableFromWorkSheet(ExcelWorksheet worksheet, bool hasHeader)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var dataTableName = $"{worksheet.Name}";
            var dataTable = new DataTable(dataTableName);

            if (worksheet.Dimension == null) return null;

            int index = 0;
            var columnsToSkip = new List<int>();
            foreach (var firstRowCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
            {
                //If a column is duplicated or empty when headers are indicated, don't include that column in the DataTable. 
                var columnName = hasHeader ? firstRowCell.Text : $"Column {firstRowCell.Start.Column}";
                if (string.IsNullOrEmpty(columnName) || dataTable.Columns.Contains(columnName))
                {
                    columnsToSkip.Add(index);
                }
                else
                {
                    dataTable.Columns.Add(columnName);
                }

                index++;
            }

            var startRow = hasHeader ? 2 : 1;

            for (var rowNumber = startRow; rowNumber <= worksheet.Dimension.End.Row; rowNumber++)
            {
                var worksheetRow = worksheet.Cells[rowNumber, 1, rowNumber, worksheet.Dimension.End.Column];
                var row = dataTable.Rows.Add();

                //Any time we skip a row we gotta modify the row index to keep it correct
                foreach (var cell in worksheetRow)
                {
                    int colIndex = cell.Start.Column - 1;
                    if (!columnsToSkip.Contains(colIndex))
                        row[GetRowColumnIndex(columnsToSkip, colIndex)] = cell.Text;
                }
            }

            return dataTable;
        }

        private static int GetRowColumnIndex(List<int> columnsToSkip, int columnNumber)
        {
            if (columnsToSkip.Any())
            {
                var skippedColumnsBeforeThis = columnsToSkip.Count(i => i <= columnNumber);
                return columnNumber - skippedColumnsBeforeThis;
            }

            return columnNumber;
        }
    }
}
