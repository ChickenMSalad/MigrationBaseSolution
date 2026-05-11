using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.WebDam.Configuration
{
    public sealed class ImportWebDamMetadataOptions
    {
        public string ExcelFilePath { get; set; } = string.Empty;
        public string? Prefix { get; set; }
        public bool SkipExisting { get; set; } = true;
        public bool CreateWebDamIdField { get; set; } = true;
    }
}
