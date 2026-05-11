using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoFieldsContainer
    {
        [JsonProperty("_links")]
        public AprimoLinks? Links { get; set; }

        [JsonProperty("items")]
        public List<AprimoField> Items { get; set; } = new();
    }
}
