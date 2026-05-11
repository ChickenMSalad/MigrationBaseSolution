using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Rules
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public sealed class SeriesFPOProductCategoryRuleSet
    {
        [JsonProperty("ruleSet")]
        public string RuleSet { get; set; } = default!;

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("matchMode")]
        public string MatchMode { get; set; } = "contains";

        [JsonProperty("caseInsensitive")]
        public bool CaseInsensitive { get; set; } = true;

        [JsonProperty("rules")]
        public List<SeriesFPOProductCategoryRule> Rules { get; set; } = new();
    }

    public sealed class SeriesFPOProductCategoryRule
    {
        [JsonProperty("aemFolderName")]
        public string AemFolderName { get; set; } = default!;

        [JsonProperty("aprimoProductCategory")]
        public string? AprimoProductCategory { get; set; }
    }


}
