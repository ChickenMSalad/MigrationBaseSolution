namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorManifestRecordItem
{
    public Guid ManifestRecordId { get; init; }

    public Guid RunId { get; init; }

    public long SequenceNumber { get; init; }

    public string SourceId { get; init; } = string.Empty;

    public string? SourcePath { get; init; }

    public string? SourceName { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
