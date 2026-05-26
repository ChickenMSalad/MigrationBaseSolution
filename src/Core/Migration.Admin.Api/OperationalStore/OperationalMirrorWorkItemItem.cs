namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorWorkItemItem
{
    public long WorkItemId { get; init; }

    public Guid RunId { get; init; }

    public long ManifestRecordId { get; init; }

    public string Status { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public string? LockedBy { get; init; }

    public DateTimeOffset CreatedAt { get; init; }


}
