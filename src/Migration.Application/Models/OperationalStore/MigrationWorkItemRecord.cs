namespace Migration.Application.Models.OperationalStore;

public sealed class MigrationWorkItemRecord
{
    public Guid WorkItemId { get; init; }

    public Guid RunId { get; init; }

    public Guid ManifestRecordId { get; init; }

    public string Status { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LockedAt { get; init; }

    public string? LockedBy { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public DateTimeOffset? FailedAt { get; init; }

    public string? LastFailureReason { get; init; }
}
