using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoRecordEmbedded
    {
        [JsonProperty("fields")]
        public AprimoFieldsContainer? Fields { get; set; }

        [JsonProperty("preview")]
        public AprimoPreview? Preview { get; set; }

        // In your sample JSON this appears as "masterfile"
        [JsonProperty("masterfile")]
        public AprimoFileVersion? Masterfile { get; set; }

        // In your sample JSON this appears as "masterfilelatestversion"
        [JsonProperty("masterfilelatestversion")]
        public AprimoFileVersion? MasterfileLatestVersion { get; set; }
    }
}
