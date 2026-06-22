namespace Migration.Domain.Models;

public sealed class AssetBinary
{
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public long? Length { get; init; }
    public string? SourceUri { get; init; }
    public string? Checksum { get; init; }

    /// <summary>
    /// Optional runtime stream opener for connector-owned binary sources.
    /// This keeps vendor targets from knowing how to resolve source-specific storage.
    /// For Azure intermediate storage, the AzureBlob source connector sets this to a blob stream
    /// resolved by the same source_asset_id tag convention written by the AzureBlob target connector.
    /// </summary>
    public Func<CancellationToken, Task<Stream>>? OpenReadAsync { get; init; }
}
