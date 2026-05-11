using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
namespace Migration.Connectors.Sources.Sitecore.Models
{
    public sealed class NodeBynderFlatRowResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("dateModifiedFrom")]
        public string? DateModifiedFrom { get; set; }

        [JsonProperty("rows")]
        public List<Dictionary<string, string>> Rows { get; set; } = new();

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("status")]
        public int? Status { get; set; }
    }
}
