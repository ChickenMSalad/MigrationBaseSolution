using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Bynder.Models
{
    public sealed class BynderMetadataImportResult
    {
        public List<string> CreatedMetaproperties { get; } = new();
        public List<string> SkippedMetaproperties { get; } = new();
        public List<string> CreatedOptions { get; } = new();
    }
}
