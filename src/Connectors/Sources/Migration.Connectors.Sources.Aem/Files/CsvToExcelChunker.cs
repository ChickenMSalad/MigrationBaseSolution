using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;

namespace Migration.Connectors.Sources.Aem.Files
{
    public class CsvToExcelChunker
    {
        public void SplitCsvToExcelChunks(
            string csvPath,
            string outputDirectory,
            int rowsPerFile = 100_000)
        {
            if (!File.Exists(csvPath))
                throw new FileNotFoundException("CSV file not found.", csvPath);

            Directory.CreateDirectory(outputDirectory);

            var baseName = Path.GetFileNameWithoutExtension(csvPath);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = "~",
                IgnoreBlankLines = true,
                BadDataFound = null,
                MissingFieldFound = null
            };

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, config);

            // 1. Read header row
            if (!csv.Read())
                throw new InvalidOperationException("CSV file appears to be empty.");

            csv.ReadHeader();
            var header = csv.HeaderRecord?.ToList() ?? new List<string>();
            if (header.Count == 0)
                throw new InvalidOperationException("CSV header row is missing or invalid.");

            var tailBuffer = new Queue<string[]>();

            int dataRowIndex = 0;
            int globalWrittenRow = 0;

            ExcelPackage? currentPackage = null;
            ExcelWorksheet? currentSheet = null;
            int currentExcelRow = 1;
            int currentChunkDataRows = 0;
            int currentChunkStartRowIndex = 1;

            void StartNewExcelChunk()
            {
                currentPackage = new ExcelPackage();
                currentSheet = currentPackage.Workbook.Worksheets.Add("Data");
                currentExcelRow = 1;
                currentChunkDataRows = 0;
                currentChunkStartRowIndex = globalWrittenRow + 1;

                // header
                for (int col = 0; col < header.Count; col++)
                {
                    currentSheet.Cells[currentExcelRow, col + 1].Value = header[col];
                }

                currentExcelRow++;
            }

            void SaveAndDisposeCurrentChunk()
            {
                if (currentPackage == null)
                    return;

                var fileName = $"{baseName}_{currentChunkStartRowIndex}.xlsx";
                var outputPath = Path.Combine(outputDirectory, fileName);
                currentPackage.SaveAs(new FileInfo(outputPath));
                currentPackage.Dispose();

                currentPackage = null;
                currentSheet = null;
            }

            // 2. Read all data rows
            while (csv.Read())
            {
                dataRowIndex++;

                // Skip the second physical row (first data row), which is garbage
                if (dataRowIndex == 1)
                {
                    continue;
                }

                int fieldCount = csv.Parser.Count;
                if (fieldCount == 0)
                {
                    continue;
                }

                var rowValues = new string[header.Count];
                for (int i = 0; i < header.Count; i++)
                {
                    if (i < fieldCount)
                    {
                        var raw = csv.GetField(i);
                        rowValues[i] = NormalizeDbNull(raw);
                    }
                    else
                    {
                        rowValues[i] = string.Empty;
                    }
                }

                // buffer for dropping last 2 rows
                tailBuffer.Enqueue(rowValues);
                if (tailBuffer.Count > 2)
                {
                    var toWrite = tailBuffer.Dequeue();

                    if (currentPackage == null || currentSheet == null)
                    {
                        StartNewExcelChunk();
                    }

                    if (currentChunkDataRows >= rowsPerFile)
                    {
                        SaveAndDisposeCurrentChunk();
                        StartNewExcelChunk();
                    }

                    for (int col = 0; col < header.Count; col++)
                    {
                        currentSheet!.Cells[currentExcelRow, col + 1].Value = toWrite[col];
                    }

                    currentExcelRow++;
                    currentChunkDataRows++;
                    globalWrittenRow++;
                }
            }

            // last 2 buffered rows are intentionally dropped
            SaveAndDisposeCurrentChunk();
        }

        private static string NormalizeDbNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value
                .Trim()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");

            return string.Equals(value, "NULL", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : value;
        }
    }



}
