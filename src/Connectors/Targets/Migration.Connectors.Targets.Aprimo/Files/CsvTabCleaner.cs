using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Files
{
    class CsvTabCleaner
    {
        // Call this with your paths:
        // CsvTabCleaner.RemoveTabsFromFile("input.csv", "output_no_tabs.csv");
        public static void RemoveTabsFromFile(string inputPath, string outputPath)
        {
            // Adjust encoding if needed (UTF8 is usually right for csv)
            using var reader = new StreamReader(inputPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);

            string? line;
            long lineNumber = 0;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;

                // Replace tabs with nothing (or change "" to "," if you want commas instead)
                string cleaned = line.Replace("\t", "");

                writer.WriteLine(cleaned);

                // Optional: log some progress occasionally for big files
                if (lineNumber % 500_000 == 0)
                {
                    Console.WriteLine($"Processed {lineNumber:N0} lines...");
                }
            }

            writer.Flush();
            Console.WriteLine("Done.");
        }

        // Optional helper to replace original file after cleaning
        public static void CleanInPlace(string path)
        {
            string tempPath = path + ".notabs";

            RemoveTabsFromFile(path, tempPath);

            // Replace original with cleaned file (keeps a backup)
            string backupPath = path + ".bak";
            File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);

            // If you don't want the backup, you can delete it:
            // File.Delete(backupPath);
        }


    }

}
