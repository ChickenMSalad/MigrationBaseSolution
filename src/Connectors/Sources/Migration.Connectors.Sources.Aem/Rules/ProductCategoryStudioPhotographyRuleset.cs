using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Rules
{
    public sealed class ProductCategoryStudioPhotographyRuleSet
    {
        public string RuleSet { get; set; } = default!;
        public int Version { get; set; }
        public string MatchMode { get; set; } = "contains";
        public bool CaseInsensitive { get; set; } = true;
        public List<ProductCategoryStudioPhotographyRule> Rules { get; set; } = new();
    }

    public sealed class ProductCategoryStudioPhotographyRule
    {
        public string AemFolderName { get; set; } = default!;
        public string? AprimoProductCategory { get; set; }
        public RuleFlags? Flags { get; set; }
    }

    public sealed class RuleFlags
    {
        public bool NoMappingProvided { get; set; }
    }

}
