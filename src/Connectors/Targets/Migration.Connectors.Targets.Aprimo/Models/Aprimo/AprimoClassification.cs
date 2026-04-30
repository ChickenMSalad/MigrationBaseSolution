using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoClassification
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }          // if Aprimo returns it

        [JsonProperty("labels")]
        public List<AprimoLabel> Labels { get; set; } = new();

        [JsonProperty("_embedded")]
        public AprimoClassificationEmbedded? Embedded { get; set; }

    }

    public sealed class AprimoClassificationEmbedded
    {
        // single parent classification node
        [JsonProperty("parent")]
        public AprimoClassification? Parent { get; set; }

        // list of child nodes if you request it
        [JsonProperty("children")]
        public AprimoClassificationChildren Children { get; set; } = new();
    }

    public sealed class AprimoClassificationChildren
    {
        [JsonProperty("items")]
        public List<AprimoClassification> Items { get; set; } = new();
    }

}
