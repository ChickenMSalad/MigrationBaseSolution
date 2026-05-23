namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker.Capacity;

public sealed class AzureWorkerCapacitySnapshot
{
    public string WorkerId { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public int ActiveWorkItems { get; init; }

    public int ActiveRuns { get; init; }

    public int VisibleQueueDepth { get; init; }

    public int LeaseRenewalCount { get; init; }

    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public Dictionary<string, string> Dimensions { get; } = new(StringComparer.OrdinalIgnoreCase);
}
