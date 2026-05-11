using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using OfficeOpenXml;
namespace Migration.Shared.Files
{
    public class StateCityFiller
    {
        public static void ProcessExcel(
            string statesTxtPath,
            string citiesTxtPath,
            string excelPath,
            string outputPath)
        {

            // Read states and cities into lists
            var states = File.ReadAllLines(statesTxtPath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            var cities = File.ReadAllLines(citiesTxtPath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            using var package = new ExcelPackage(new FileInfo(excelPath));
            var ws = package.Workbook.Worksheets[0];

            // Find column indexes (case-insensitive)
            var headerRow = 1;
            int colCount = ws.Dimension.End.Column;

            int assetFolderCol = FindCol(ws, headerRow, "AssetFolder");
            int azureFilenameCol = FindCol(ws, headerRow, "AzureFilename");
            int stateCol = FindCol(ws, headerRow, "State");
            int cityCol = FindCol(ws, headerRow, "City");

            for (int row = 2; row <= ws.Dimension.End.Row; row++)
            {
                string assetFolder = ws.Cells[row, assetFolderCol].Text ?? "";
                string azureFilename = ws.Cells[row, azureFilenameCol].Text ?? "";

                // Check states
                var foundState = states.FirstOrDefault(state =>
                    assetFolder.Contains(state, StringComparison.OrdinalIgnoreCase) ||
                    azureFilename.Contains(state, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(foundState))
                {
                    ws.Cells[row, stateCol].Value = foundState;
                }

                // Check cities
                var foundCity = cities.FirstOrDefault(city =>
                    assetFolder.Contains(city, StringComparison.OrdinalIgnoreCase) ||
                    azureFilename.Contains(city, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(foundCity))
                {
                    ws.Cells[row, cityCol].Value = foundCity;
                }
            }

            package.SaveAs(new FileInfo(outputPath));
        }

        // Helper: find column by name
        private static int FindCol(ExcelWorksheet ws, int row, string colName)
        {
            for (int col = 1; col <= ws.Dimension.End.Column; col++)
            {
                if (string.Equals(ws.Cells[row, col].Text, colName, StringComparison.OrdinalIgnoreCase))
                    return col;
            }
            throw new Exception($"Column '{colName}' not found!");
        }
    }
}

