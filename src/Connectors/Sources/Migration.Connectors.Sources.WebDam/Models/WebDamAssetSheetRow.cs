using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.WebDam.Models
{
    public sealed class WebDamAssetSheetRow
    {
        public string AssetId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public long? SizeBytes { get; set; }
        public string? FileType { get; set; }
        public string? FolderId { get; set; }
        public string? FolderPath { get; set; }
    }
}
