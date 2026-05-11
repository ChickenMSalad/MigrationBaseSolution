using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Rules
{
    public sealed class ItemStatusRuleSet
    {
        public string RuleSet { get; set; } = default!;
        public int Version { get; set; }
        public string MatchMode { get; set; } = "contains";
        public bool CaseInsensitive { get; set; } = true;
        public List<ItemStatusRule> Rules { get; set; } = new();
    }

    public sealed class ItemStatusRule
    {
        public string CurrentValue { get; set; } = default!;
        public string? AprimoValue { get; set; }
    }

}
