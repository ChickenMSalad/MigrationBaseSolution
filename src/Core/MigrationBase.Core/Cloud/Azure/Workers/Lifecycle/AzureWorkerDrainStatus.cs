namespace MigrationBase.Core.Cloud.Azure.Workers.Lifecycle;

/// <summary>
/// Captures worker drain progress without requiring a concrete queue or host implementation.
/// </summary>
public sealed class AzureWorkerDrainStatus
{
    public string WorkerId { get; set; } = string.Empty;

    public bool IsDrainRequested { get; set; }

    public bool IsAcceptingNewWork { get; set; } = true;

    public int ActiveWorkItemCount { get; set; }

    public int CompletedDuringDrainCount { get; set; }

    public int AbandonedDuringDrainCount { get; set; }

    public DateTimeOffset? DrainStartedAtUtc { get; set; }

    public DateTimeOffset? DrainCompletedAtUtc { get; set; }

    public string State { get; set; } = "Running";

    public string Message { get; set; } = string.Empty;
}
