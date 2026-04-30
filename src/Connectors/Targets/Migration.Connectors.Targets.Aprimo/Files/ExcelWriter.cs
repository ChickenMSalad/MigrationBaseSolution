using OfficeOpenXml;

using System.Data;

namespace Migration.Connectors.Targets.Aprimo.Files
{
    public static class ExcelWriter
    {
        public class ColumnExtendedProperties
        {
            public const string VerticalOrientation = "VerticalOrientation";
            public const string HasDataValidation = "HasDataValidation";
            public const string DataValidationList = "DataValidationList";
        }

        private class CustomColumn
        {
            public string ColumnName { get; set; }
            public bool VerticalOrientation { get; set; }
            public bool HasDataValidation { get; set; }
            public List<string> DataValidationList { get; set; }
        }

        public static MemoryStream WriteDataTable(DataTable dataTable)
        {
            var excelMemoryStream = new MemoryStream();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                WriteDataTable(dataTable, package);
                package.SaveAs(excelMemoryStream);
            }

            excelMemoryStream.Flush();
            excelMemoryStream.Position = 0;

            return excelMemoryStream;
        }

        public static MemoryStream WriteDataTables(List<DataTable> dataTables)
        {
            var excelMemoryStream = new MemoryStream();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                WriteDataTables(dataTables, package);
                package.SaveAs(excelMemoryStream);
            }

            excelMemoryStream.Flush();
            excelMemoryStream.Position = 0;

            return excelMemoryStream;
        }

        private static void WriteDataTable(DataTable dataTable, ExcelPackage excelPackage)
        {
            var sheet = excelPackage.Workbook.Worksheets.Add(dataTable.TableName ?? "Sheet1"); // temporary name

            WriteDataTable(dataTable, sheet);

            excelPackage.Save();
        }

        private static void WriteDataTables(List<DataTable> dataTables, ExcelPackage excelPackage)
        {
            foreach (var dataTable in dataTables)
            {
                var sheet = excelPackage.Workbook.Worksheets.Add(string.IsNullOrEmpty(dataTable.TableName) ? "Sheet1" : dataTable.TableName);

                WriteDataTable(dataTable, sheet);
            }

            excelPackage.Save();
        }

        private static void WriteDataTable(DataTable dataTable, ExcelWorksheet sheet)
        {
            sheet.DefaultColWidth = 3;

            var columns = new List<CustomColumn>();

            foreach (DataColumn column in dataTable.Columns)
            {
                columns.Add(new CustomColumn
                {
                    ColumnName = column.ColumnName,
                    VerticalOrientation = column.ExtendedProperties[ColumnExtendedProperties.VerticalOrientation] as bool? ?? false,
                    HasDataValidation = column.ExtendedProperties[ColumnExtendedProperties.HasDataValidation] as bool? ?? false,
                    DataValidationList = column.ExtendedProperties[ColumnExtendedProperties.DataValidationList] as List<string> ?? new List<string>()
                });
            }

            WriteDataRow(sheet, 1, columns.Select(column => column.ColumnName));

            foreach (var customColumn in columns)
            {
                if (!customColumn.VerticalOrientation) continue;

                var cell = sheet.Cells["1:1"].First(column => column.Value.ToString() == customColumn.ColumnName).Start;
                //sheet.Cells[cell.Address].Style.TextRotation = 90; // stop using because it triggers GDI error in linux
            }

            var row = 2;
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var values = dataRow.ItemArray.Select(item => item.ToString());
                WriteDataRow(sheet, row++, values);
            }

            foreach (var customColumn in columns)
            {
                if (!customColumn.HasDataValidation) continue;

                var excelRange = sheet.Cells["1:1"].First(column => column.Value.ToString() == customColumn.ColumnName);
                var cell = excelRange.Start;

                var cellStart = new ExcelCellAddress(2, cell.Column);
                var cellEnd = new ExcelCellAddress(row + 100, cell.Column);
                var addressRange = $"{cellStart.Address}:{cellEnd.Address}";

                var validation = sheet.DataValidations.AddListValidation(addressRange);
                validation.ShowErrorMessage = true;

                foreach (var value in customColumn.DataValidationList)
                {
                    validation.Formula.Values.Add(value);
                }
            }

            // commented due to GDI error in linux
            //for (var col = 1; col < dataTable.Columns.Count + 1; col++)
            //{
            //    sheet.Column(col).AutoFit();
            //}
        }

        private static void WriteDataRow(ExcelWorksheet sheet, int row, IEnumerable<string> items)
        {
            var itemsList = items as IList<string> ?? items.ToList();

            var col = 1;
            foreach (var item in itemsList)
            {
                sheet.Cells[row, col++].Value = item;
            }
        }
    }
}