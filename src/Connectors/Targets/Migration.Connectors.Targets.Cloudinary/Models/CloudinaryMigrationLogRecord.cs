namespace Migration.Connectors.Targets.Cloudinary.Models;

public sealed class CloudinaryMigrationLogRecord
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public string RowId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? PublicId { get; init; }
    public string? AssetId { get; init; }
    public string? SecureUrl { get; init; }
    public string? Message { get; init; }
    public object? Request { get; init; }
    public object? Response { get; init; }
}
