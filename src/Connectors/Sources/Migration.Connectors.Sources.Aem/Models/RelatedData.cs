using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Models
{
    public class RelatedData
    {
        [JsonProperty("jcr:primaryType")]
        public string PrimaryType { get; set; }

        [JsonProperty("sling:resources")]
        public List<string> Resources { get; set; }
    }
}
