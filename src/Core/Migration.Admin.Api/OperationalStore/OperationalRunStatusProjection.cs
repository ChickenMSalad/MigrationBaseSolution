namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunStatusProjection
{
    public Guid RunId { get; init; }

    public string SourceSystem { get; init; } = string.Empty;

    public string TargetSystem { get; init; } = string.Empty;

    public string RunStatus { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public DateTimeOffset? FailedAt { get; init; }

    public string? FailureReason { get; init; }

    public int ManifestRecordCount { get; init; }

    public int ManifestCreatedCount { get; init; }

    public int ManifestProcessingCount { get; init; }

    public int ManifestCompletedCount { get; init; }

    public int ManifestFailedCount { get; init; }

    public int WorkItemCount { get; init; }

    public int WorkItemCreatedCount { get; init; }

    public int WorkItemLockedCount { get; init; }

    public int WorkItemProcessingCount { get; init; }

    public int WorkItemCompletedCount { get; init; }

    public int WorkItemFailedCount { get; init; }

    public int FailureCount { get; init; }

    public int CheckpointCount { get; init; }

    public decimal CompletionPercent { get; init; }

    public string ProjectionStatus { get; init; } = string.Empty;
}


