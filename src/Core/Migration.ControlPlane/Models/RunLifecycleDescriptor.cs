namespace Migration.ControlPlane.Models;

/// <summary>
/// Cloud-facing lifecycle projection for a migration run.
/// This is intentionally derived from the existing run control record instead
/// of replacing current run status storage.
/// </summary>
public sealed record RunLifecycleDescriptor(
    string RunId,
    string Status,
    string LifecycleStage,
    bool IsPreflight,
    bool IsTerminal,
    bool CanCancel,
    bool CanRetry,
    bool CanRetryFailures,
    bool CanResume,
    bool CanViewWorkItems,
    string? Message,
    DateTimeOffset UpdatedUtc,
    DateTimeOffset? CompletedUtc);

public static class RunLifecycleStages
{
    public const string Unknown = "unknown";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Canceled = "canceled";
}
