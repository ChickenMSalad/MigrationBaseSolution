namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherEligibleWorkItemPreview
{
    public Guid WorkItemId { get; init; }

    public Guid RunId { get; init; }

    public Guid ManifestRecordId { get; init; }

    public string RunStatus { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string SourceId { get; init; } = string.Empty;

    public string? SourceName { get; init; }
}
