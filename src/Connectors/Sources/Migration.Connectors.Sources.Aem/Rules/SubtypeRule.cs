using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Rules
{
    public sealed class SubtypeRule
    {
        public string Id { get; set; } = default!;

        // Match if base filename (no extension) ends with ANY of these suffixes
        public List<string>? EndsWithAny { get; set; }

        // Match only if base filename does NOT end with ANY of these suffixes
        public List<string>? NotEndsWithAny { get; set; }

        // Optional: additional "contains" checks against the base filename (no extension)
        public List<string>? ContainsAny { get; set; }

        // Optional: additional "does not contain" checks against the base filename (no extension)
        public List<string>? NotContainsAny { get; set; }

        public string Subtype { get; set; } = default!;

        public bool RequiresAprimoAI { get; set; }
        public bool IsDefault { get; set; }

        public int Priority { get; set; } = 0;
        public string? Notes { get; set; }
    }

}
