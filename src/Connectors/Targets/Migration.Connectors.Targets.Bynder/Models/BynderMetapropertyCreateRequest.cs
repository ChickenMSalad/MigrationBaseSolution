using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Bynder.Models
{
    public sealed class BynderMetapropertyCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public int? Position { get; set; }
        public bool IsSearchable { get; set; }
        public List<string> Options { get; set; } = new();
    }
}
