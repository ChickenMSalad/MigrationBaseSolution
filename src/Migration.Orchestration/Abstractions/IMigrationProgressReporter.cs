namespace Migration.Orchestration.Abstractions;

public interface IMigrationProgressReporter
{
    Task ReportAsync(MigrationProgressEvent progressEvent, CancellationToken cancellationToken = default);
}

public sealed class MigrationProgressEvent
{
    public required string RunId { get; init; }
    public required string JobName { get; init; }
    public required string EventName { get; init; }
    public string? WorkItemId { get; init; }
    public int? Completed { get; init; }
    public int? Total { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string?> Properties { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class MigrationProgressEvents
{
    public const string RunStarted = "RunStarted";
    public const string ManifestLoaded = "ManifestLoaded";
    public const string WorkItemStarted = "WorkItemStarted";
    public const string WorkItemSkipped = "WorkItemSkipped";
    public const string SourceReadCompleted = "SourceReadCompleted";
    public const string TargetUpsertCompleted = "TargetUpsertCompleted";
    public const string WorkItemFailed = "WorkItemFailed";
    public const string WorkItemSucceeded = "WorkItemSucceeded";
    public const string RunCompleted = "RunCompleted";
}
