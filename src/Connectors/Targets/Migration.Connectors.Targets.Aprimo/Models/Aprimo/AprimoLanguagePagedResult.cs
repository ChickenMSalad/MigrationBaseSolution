using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoLanguagePagedResult
    {
        [JsonProperty("items")]
        public List<AprimoLanguage> Items { get; set; } = new();

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
    }

}
