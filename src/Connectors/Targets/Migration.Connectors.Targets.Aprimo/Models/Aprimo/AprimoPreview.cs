using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoPreview
    {
        [JsonProperty("_links")]
        public AprimoLinks? Links { get; set; }

        [JsonProperty("size")]
        public long? Size { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("extension")]
        public string? Extension { get; set; }

        [JsonProperty("uri")]
        public string? Uri { get; set; }
    }
}
