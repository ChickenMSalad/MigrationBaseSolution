using Migration.Domain.Enums;

namespace Migration.Domain.Models;

public sealed class AssetEnvelope
{
    public required string SourceAssetId { get; init; }
    public string? ExternalId { get; init; }
    public string? Path { get; init; }
    public ConnectorType SourceType { get; init; }
    public Dictionary<string, string?> Metadata { get; init; } = new();
    public AssetBinary? Binary { get; init; }
    public List<RenditionRecord> Renditions { get; init; } = new();
}
