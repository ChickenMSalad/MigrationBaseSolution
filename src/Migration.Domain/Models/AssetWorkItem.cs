namespace Migration.Domain.Models;

public sealed class AssetWorkItem
{
    public required string WorkItemId { get; init; }
    public required ManifestRow Manifest { get; init; }
    public AssetEnvelope? SourceAsset { get; set; }
    public TargetAssetPayload? TargetPayload { get; set; }
}
