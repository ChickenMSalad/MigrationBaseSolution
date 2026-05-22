using CsvHelper;
using CsvHelper.Configuration;

using System.Data;
using System.Globalization;

namespace Migration.Shared.Files
{
    public static class CsvUtils
    {
        public static DataTable GetDictionaryDataFromCSV(string csvFilePath)
        {
            if (string.IsNullOrWhiteSpace(csvFilePath))
                throw new ArgumentException("CSV file path is required.", nameof(csvFilePath));

            if (!File.Exists(csvFilePath))
                throw new FileNotFoundException("CSV file not found.", csvFilePath);

            var table = new DataTable();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, config);

            // Read header
            csv.Read();
            csv.ReadHeader();

            foreach (var header in csv.HeaderRecord!)
                table.Columns.Add(header, typeof(string));

            // Read rows as dictionary<string, string>
            while (csv.Read())
            {
                var dict = csv.GetRecord<dynamic>() as IDictionary<string, object>;
                var row = table.NewRow();

                foreach (DataColumn col in table.Columns)
                {
                    row[col.ColumnName] =
                        dict != null && dict.TryGetValue(col.ColumnName, out var value)
                            ? value?.ToString()
                            : DBNull.Value;
                }

                table.Rows.Add(row);
            }

            return table;
        }
    }

}
