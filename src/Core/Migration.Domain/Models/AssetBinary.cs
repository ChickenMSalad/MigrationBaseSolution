namespace Migration.Domain.Models;

public sealed class AssetBinary
{
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public long? Length { get; init; }
    public string? SourceUri { get; init; }
    public string? Checksum { get; init; }
}
