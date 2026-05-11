using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Rules
{
    public sealed class VendorSubtypeRuleSet
    {
        public string RuleSetId { get; set; } = default!;
        public int Version { get; set; }
        public string MatchMode { get; set; } = "PathContainsIgnoreCase";
        public bool PreferMostSpecificMatch { get; set; } = true;
        public List<VendorSubtypeRule> Rules { get; set; } = new();
    }

    public sealed class VendorSubtypeRule
    {
        public string AemFolderToken { get; set; } = default!;
        public string? VendorName { get; set; }
        public string? Subtype { get; set; }
    }

}
