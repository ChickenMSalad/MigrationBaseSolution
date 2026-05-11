using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoLocalizedValue
    {
        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("values")]
        public List<string>? Values { get; set; }

        // RecordLink case
        [JsonProperty("links")]
        public object? Links { get; set; }

        [JsonProperty("parents")]
        public object? Parents { get; set; }

        [JsonProperty("children")]
        public object? Children { get; set; }

        [JsonProperty("aiInfluenced")]
        public bool? AiInfluenced { get; set; }

        [JsonProperty("languageId")]
        public string? LanguageId { get; set; }

        [JsonProperty("readOnly")]
        public object? ReadOnly { get; set; }

        [JsonProperty("modifiedOn")]
        public DateTimeOffset? ModifiedOn { get; set; }

        // Present on Date fields in your sample
        [JsonProperty("hasDay")]
        public bool? HasDay { get; set; }

        [JsonProperty("hasMonth")]
        public bool? HasMonth { get; set; }
    }

}
