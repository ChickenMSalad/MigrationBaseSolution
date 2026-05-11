using Newtonsoft.Json;

namespace Migration.Hosts.Ashley.AemToAprimo.Functions.Models;

public sealed class StorageBlobCreatedEventData
{
    [JsonProperty("url")]
    public string? Url { get; set; }
}
