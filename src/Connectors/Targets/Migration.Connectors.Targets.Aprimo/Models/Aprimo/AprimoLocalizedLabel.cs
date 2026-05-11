using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoLocalizedLabel
    {
        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("languageId")]
        public string? LanguageId { get; set; }
    }
}
