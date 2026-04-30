using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public class AprimoRecord
    {
        [JsonProperty("_links")]
        public AprimoLinks? Links { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("contentType")]
        public string? ContentType { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("tag")]
        public string? Tag { get; set; }

        [JsonProperty("textContent")]
        public string? TextContent { get; set; }

        [JsonProperty("aiInfluenced")]
        public string? AiInfluenced { get; set; }

        [JsonProperty("hasImageOverlay")]
        public bool HasImageOverlay { get; set; }

        [JsonProperty("modifiedOn")]
        public DateTimeOffset? ModifiedOn { get; set; }

        [JsonProperty("createdOn")]
        public DateTimeOffset? CreatedOn { get; set; }

        [JsonProperty("_embedded")]
        public AprimoRecordEmbedded? Embedded { get; set; }
    }

}
