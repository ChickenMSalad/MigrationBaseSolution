using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public class AprimoSearchResponse<T>
    {
        [JsonProperty("items")]
        public List<T>? Items { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }
    }
}
