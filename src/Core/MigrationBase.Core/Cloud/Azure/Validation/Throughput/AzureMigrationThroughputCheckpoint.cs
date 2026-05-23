namespace MigrationBase.Core.Cloud.Azure.Validation.Throughput;

public sealed class AzureMigrationThroughputCheckpoint
{
    public string RunId { get; init; } = string.Empty;
    public DateTimeOffset CapturedAtUtc { get; init; }
    public int CompletedItems { get; init; }
    public int FailedItems { get; init; }
    public int ActiveWorkers { get; init; }
    public int PendingWorkItems { get; init; }
    public int LeasedWorkItems { get; init; }
    public double ItemsPerMinute { get; init; }
}
