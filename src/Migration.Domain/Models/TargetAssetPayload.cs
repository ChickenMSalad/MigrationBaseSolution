using Migration.Domain.Enums;

namespace Migration.Domain.Models;

public sealed class TargetAssetPayload
{
    public ConnectorType TargetType { get; init; }
    public string? Name { get; set; }
    public Dictionary<string, object?> Fields { get; init; } = new();
    public AssetBinary? Binary { get; set; }
}
