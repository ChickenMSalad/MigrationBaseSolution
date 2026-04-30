using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Rules
{
    public sealed class ProductCategoryAHSVideoRuleSet
    {
        public string RuleSet { get; set; } = default!;
        public int Version { get; set; }
        public string MatchMode { get; set; } = "contains";
        public bool CaseInsensitive { get; set; } = true;
        public List<ProductCategoryAHSVideoRule> Rules { get; set; } = new();
    }

    public sealed class ProductCategoryAHSVideoRule
    {
        public string AemFolderName { get; set; } = default!;
        public string? AprimoProductCategory { get; set; }
        public RuleFlags? Flags { get; set; }
    }

}
