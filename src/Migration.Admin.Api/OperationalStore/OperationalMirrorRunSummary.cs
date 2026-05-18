namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorRunSummary
{
    public Guid RunId { get; init; }

    public string SourceSystem { get; init; } = string.Empty;

    public string TargetSystem { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public int ManifestRecordCount { get; init; }

    public int WorkItemCount { get; init; }

    public int CheckpointCount { get; init; }
}
