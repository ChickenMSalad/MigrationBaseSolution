using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoLanguage
    {
        [JsonProperty("id")]
        public string Id { get; set; } = default!;

        // e.g. "en_US", "fr_FR"
        [JsonProperty("culture")]
        public string Code { get; set; } = default!;

        [JsonProperty("name")]
        public string? Name { get; set; }

    }

}
