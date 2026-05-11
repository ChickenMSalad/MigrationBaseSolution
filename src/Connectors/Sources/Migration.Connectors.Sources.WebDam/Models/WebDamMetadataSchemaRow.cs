using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.WebDam.Models
{
    public sealed class WebDamMetadataSchemaRow
    {
        public string Field { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? Searchable { get; set; }
        public string? Position { get; set; }
        public string? PossibleValues { get; set; }
    }
}
