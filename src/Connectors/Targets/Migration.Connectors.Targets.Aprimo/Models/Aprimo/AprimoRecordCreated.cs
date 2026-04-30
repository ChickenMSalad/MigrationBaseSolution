using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoRecordCreated
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }
    }
}
