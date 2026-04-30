using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Bynder.Models
{
    public sealed class CreateBynderImportFileOptions
    {
        public string SourceExcelFilePath { get; set; } = string.Empty;
        public string OutputExcelFilePath { get; set; } = string.Empty;
        public string? Prefix { get; set; }
    }
}
