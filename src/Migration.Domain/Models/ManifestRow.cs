namespace Migration.Domain.Models;

public sealed class ManifestRow
{
    public required string RowId { get; init; }
    public string? SourceAssetId { get; init; }
    public string? SourcePath { get; init; }
    public Dictionary<string, string?> Columns { get; init; } = new();
}
