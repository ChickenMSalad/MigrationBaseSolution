using Newtonsoft.Json;

namespace Migration.Hosts.WebDamToBynder.Functions.Models;

public sealed class StorageBlobCreatedEventData
{
    [JsonProperty("url")]
    public string? Url { get; set; }
}
