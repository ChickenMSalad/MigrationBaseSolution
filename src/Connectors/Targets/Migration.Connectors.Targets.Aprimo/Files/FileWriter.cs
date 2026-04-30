using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Files
{
    public class FileWriter
    {
        private static void WriteFileOut(string sourceDirectory, string filename, DataTable masterDataTable)
        {
            var stream = new MemoryStream();
            CsvDataHelper.WriteDataTable(stream, masterDataTable);

            var fileName = $"{sourceDirectory}{filename}";

            using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            stream.WriteTo(fs);
        }
    }
}
