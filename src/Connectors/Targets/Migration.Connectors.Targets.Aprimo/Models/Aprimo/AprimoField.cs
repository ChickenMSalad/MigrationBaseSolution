using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoField
    {
        [JsonProperty("_links")]
        public AprimoLinks? Links { get; set; }

        [JsonProperty("dataType")]
        public string? DataType { get; set; }

        [JsonProperty("fieldName")]
        public string? FieldName { get; set; }

        [JsonProperty("label")]
        public string? Label { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("localizedValues")]
        public List<AprimoLocalizedValue>? LocalizedValues { get; set; }

        [JsonProperty("inheritanceState")]
        public object? InheritanceState { get; set; }

        [JsonProperty("inheritable")]
        public object? Inheritable { get; set; }

        // Present on RecordLink fields in your sample
        [JsonProperty("recordLinkConditions")]
        public object? RecordLinkConditions { get; set; }
    }
}
