using Migration.Orchestration.Abstractions;

namespace Migration.ControlPlane.Models;

public sealed record RunSummaryResponse
{
    public required string RunId { get; init; }
    public required string ProjectId { get; init; }
    public required string JobName { get; init; }
    public required string Status { get; init; }
    public bool DryRun { get; init; }
    public DateTimeOffset CreatedUtc { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; }
    public DateTimeOffset? StartedUtc { get; init; }
    public DateTimeOffset? CompletedUtc { get; init; }
    public string? Message { get; init; }
    public int TotalWorkItems { get; init; }
    public int Succeeded { get; init; }
    public int DryRunSucceeded { get; init; }
    public int Failed { get; init; }
    public int ValidationFailed { get; init; }
    public int Running { get; init; }
    public int Skipped { get; init; }
    public int Pending { get; init; }
    public decimal? PercentComplete { get; init; }
    public IReadOnlyDictionary<string, int> StatusCounts { get; init; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<RunFailureSummary> RecentFailures { get; init; } = Array.Empty<RunFailureSummary>();
}

public sealed record RunFailureSummary
{
    public required string WorkItemId { get; init; }
    public string? SourceAssetId { get; init; }
    public string? TargetAssetId { get; init; }
    public required string Status { get; init; }
    public string? Message { get; init; }
    public string? LastError { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; }
}

public sealed record RunEventsResponse(string RunId, int Count, IReadOnlyList<RunProgressEventRecord> Events);
public sealed record RunFailuresResponse(string RunId, int Count, IReadOnlyList<RunFailureSummary> Failures);

public sealed record RunProgressEventRecord
{
    public required string EventId { get; init; }
    public required string RunId { get; init; }
    public required string JobName { get; init; }
    public required string EventName { get; init; }
    public string? WorkItemId { get; init; }
    public int? Completed { get; init; }
    public int? Total { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public Dictionary<string, string?> Properties { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
