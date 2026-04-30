using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Migration.Connectors.Sources.Aem.Models;

public class AemJcrContent
{
    [JsonProperty("dam:s7damType")]
    public string DamS7DamType { get; set; }

    [JsonProperty("jcr:lastModifiedBy")]
    public string JcrLastModifiedBy { get; set; }

    [JsonProperty("dam:processingAttempts")]
    public int DamProcessingAttempts { get; set; }

    [JsonProperty("dam:asyncJobId")]
    public string DamAsyncJobId { get; set; }

    [JsonProperty("cq:isDelivered")]
    public bool CqIsDelivered { get; set; }

    [JsonProperty("dam:processingId")]
    public string DamProcessingId { get; set; }

    [JsonProperty("dam:assetState")]
    public string DamAssetState { get; set; }

    [JsonProperty("dam:runPostProcess")]
    public bool DamRunPostProcess { get; set; }

    [JsonProperty("dam:scene7QueuedWith")]
    public string DamScene7QueuedWith { get; set; }

    [JsonProperty("dam:scene7RequestID")]
    public string DamScene7RequestId { get; set; }

    [JsonProperty("cq:parentPath")]
    public string CqParentPath { get; set; }

    [JsonProperty("cq:name")]
    public string CqName { get; set; }

    [JsonProperty("dam:scene7BatchId")]
    public string DamScene7BatchId { get; set; }

    // AEM date strings are NOT ISO 8601 — keep as string unless you custom-parse
    [JsonProperty("jcr:lastModified")]
    public string JcrLastModified { get; set; }

    [JsonProperty("dam:processingRenditions")]
    public List<string> DamProcessingRenditions { get; set; }

    [JsonProperty("dam:processingRequestedDate")]
    public string DamProcessingRequestedDate { get; set; }

    [JsonProperty("jcr:primaryType")]
    public string JcrPrimaryType { get; set; }

    [JsonProperty("dam:runDMProcess")]
    public bool DamRunDmProcess { get; set; }

    [JsonProperty("dam:imageServerAsset")]
    public bool DamImageServerAsset { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
}
