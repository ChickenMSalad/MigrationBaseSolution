using System.Data;
using System.Globalization;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

namespace Migration.Shared.Files
{
    public static class CsvDataHelper
    {
        private static CsvConfiguration GetDefaultConfiguration() 
        {
            return new CsvConfiguration(CultureInfo.InvariantCulture);
        }

        public static CsvConfiguration GetCsvConfiguration(string delimiter, bool hasHeader = true)
        {
            return new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter, HasHeaderRecord = hasHeader, MissingFieldFound = null, TrimOptions = TrimOptions.Trim };
        }

        #region |-- LoadDataTable --|

        public static DataTable LoadDataTable(string path)
        {
            return LoadDataTable(path, GetDefaultConfiguration());
        }

        public static DataTable LoadDataTable(string path, CsvConfiguration configuration)
        {
            using (var streamReader = new StreamReader(path))
            {
                return LoadDataTableImpl(streamReader, configuration);
            }
        }

        public static DataTable LoadDataTable(Stream stream)
        {
            return LoadDataTable(stream, GetDefaultConfiguration());
        }

        public static DataTable LoadDataTable(Stream stream, CsvConfiguration configuration)
        {
            using (var streamReader = new StreamReader(stream))
            {
                return LoadDataTableImpl(streamReader, configuration);
            }
        }

        #endregion

        #region |-- WriteDataTable --|

        public static void WriteDataTable(string path, DataTable dataTable)
        {
            WriteDataTable(path, dataTable, GetDefaultConfiguration());
        }

        public static void WriteDataTable(string path, DataTable dataTable, CsvConfiguration configuration)
        {
            using (var writer = new StreamWriter(path))
            {
                WriteDataTableImpl(writer, dataTable, configuration);
            }
        }

        public static void WriteDataTable(Stream stream, DataTable dataTable)
        {
            WriteDataTable(stream, dataTable, GetDefaultConfiguration());
        }

        public static void WriteDataTable(Stream stream, DataTable dataTable, CsvConfiguration configuration)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                WriteDataTableImpl(writer, dataTable, configuration);
            }
        }

        #endregion

        #region |-- Support Methods --|

        private static DataTable LoadDataTableImpl(TextReader reader, CsvConfiguration configuration)
        {
            using (var csvReader = new CsvReader(reader, configuration))
            using (var csvDataReader = new CsvDataReader(csvReader))
            {
                // Load data table
                var dataTable = new DataTable();
                dataTable.Load(csvDataReader);

                return dataTable;
            }
        }

        private static void WriteDataTableImpl(TextWriter writer, DataTable dataTable, CsvConfiguration configuration)
        {
            using (var csvWriter = new CsvWriter(writer, configuration))
            {
                // Write header row
                foreach (DataColumn column in dataTable.Columns)
                {
                    csvWriter.WriteField(column.ColumnName);
                }

                csvWriter.NextRecord();

                // Write data rows
                foreach (DataRow row in dataTable.Rows)
                {
                    for (var i = 0; i < dataTable.Columns.Count; i++)
                    {
                        csvWriter.WriteField(row[i]);
                    }

                    csvWriter.NextRecord();
                }
            }
        }

        #endregion
    }
}
