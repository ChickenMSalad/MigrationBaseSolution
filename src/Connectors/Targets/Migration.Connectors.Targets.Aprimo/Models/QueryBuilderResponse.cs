using Migration.Shared.Workflows.AemToAprimo.Mapping;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models
{
    public class QueryBuilderResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("results")]
        public int Results { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("more")]
        public bool More { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("hits")]
        public List<QueryHit> Hits { get; set; }
    }

    public class QueryHit
    {
        [JsonProperty("jcr:created")]
        public string Created { get; set; }

        [JsonProperty("jcr:path")]
        public string Path { get; set; }

        [JsonProperty("jcr:uuid")]
        public string Uuid { get; set; }

        [JsonProperty("jcr:content")]
        public JcrContent Content { get; set; }

        [JsonProperty("jcr:primaryType")]
        public string PrimaryType { get; set; }

        
    }

    public class JcrContent
    {
        [JsonProperty("jcr:lastModified")]
        public string LastModified { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("dam:size")]
        public long Size { get; set; }

        [JsonProperty("dam:MIMEtype")]
        public string MimeType { get; set; }

        [JsonProperty("dc:title")]
        [JsonConverter(typeof(CommaJoinStringOrArrayConverter))]
        public string Title { get; set; }
    }


}
