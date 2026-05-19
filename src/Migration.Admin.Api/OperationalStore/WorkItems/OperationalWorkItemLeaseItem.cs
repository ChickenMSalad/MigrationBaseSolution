namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalWorkItemLeaseItem
{
    public Guid WorkItemId { get; init; }

    public Guid RunId { get; init; }

    public Guid ManifestRecordId { get; init; }

    public string Status { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public string? LockedBy { get; init; }

    public DateTimeOffset? LockedAt { get; init; }

    public string SourceId { get; init; } = string.Empty;

    public string? SourcePath { get; init; }

    public string? SourceName { get; init; }
}
