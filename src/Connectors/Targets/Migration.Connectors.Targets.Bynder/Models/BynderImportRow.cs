using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Bynder.Models
{
    public sealed class BynderImportRow
    {
        public string AssetId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public long? SizeBytes { get; set; }
        public string? FileType { get; set; }
        public string? FolderId { get; set; }
        public string? FolderPath { get; set; }

        public Dictionary<string, string> Metadata { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);
    }
}
