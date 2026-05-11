using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Files
{
    class TabChecker
    {
        public static void CheckForTabs(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            string? line;
            long lineNumber = 0;
            long rowsWithTabs = 0;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                if (line.Contains('\t'))
                {
                    rowsWithTabs++;
                    if (rowsWithTabs <= 5)
                    {
                        Console.WriteLine($"Tab found on line {lineNumber}");
                    }
                }
            }

            Console.WriteLine($"Total lines with tabs: {rowsWithTabs:N0}");
            Console.WriteLine($"Total lines : {lineNumber}");
        }


    }

}
