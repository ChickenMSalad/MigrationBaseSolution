using Newtonsoft.Json;
using System.Collections.Generic;

namespace Migration.Connectors.Targets.Aprimo.Models
{
    public sealed class AprimoImageSetAssets
    {
        [JsonProperty("jcr:primaryType")]
        public string? PrimaryType { get; set; }

        [JsonProperty("sling:resources")]
        public List<string> Resources { get; set; } = new();

        public List<string> AzureResources { get; set; } = new();

        public List<string> AprimoRecords { get; set; } = new();
    }

}
