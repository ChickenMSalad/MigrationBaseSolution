using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoRecordWithFields
    {
        // Matches: "fields": { "items": [ ... ] }
        [JsonProperty("fields")]
        public AprimoFieldsContainer? Fields { get; set; }
    }
}
