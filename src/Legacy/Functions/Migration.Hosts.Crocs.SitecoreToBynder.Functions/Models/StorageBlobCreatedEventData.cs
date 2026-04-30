using Newtonsoft.Json;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Functions.Models;

public sealed class StorageBlobCreatedEventData
{
    [JsonProperty("url")]
    public string? Url { get; set; }
}
