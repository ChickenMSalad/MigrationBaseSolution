using System;
using Newtonsoft.Json;

namespace Migration.Connectors.Sources.Aem.Models;

public class AemRendition
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("uri")]
    public string Uri { get; set; }

    // ISO-8601 timestamp → safe to parse as DateTimeOffset
    [JsonProperty("jcr:created")]
    public DateTimeOffset JcrCreated { get; set; }

    // Optional: not present on every rendition
    [JsonProperty("jcr:createdBy")]
    public string JcrCreatedBy { get; set; }

    [JsonProperty("jcr:primaryType")]
    public string JcrPrimaryType { get; set; }
}
