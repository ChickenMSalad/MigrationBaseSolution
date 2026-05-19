namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineEvent
{
    public DateTimeOffset OccurredAt { get; init; }

    public string EventType { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public Guid? RunId { get; init; }

    public Guid? WorkItemId { get; init; }

    public Guid? ManifestRecordId { get; init; }

    public Guid? CheckpointId { get; init; }

    public Guid? FailureId { get; init; }

    public string Message { get; init; } = string.Empty;
}
