namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker.Capacity;

public sealed class AzureWorkerCapacityLimit
{
    public string Name { get; init; } = string.Empty;

    public int? MaxConcurrentWorkItems { get; init; }

    public int? MaxConcurrentRuns { get; init; }

    public int? MaxQueueDepth { get; init; }

    public int? MaxLeaseRenewals { get; init; }

    public TimeSpan? MaxWorkItemAge { get; init; }

    public bool IsEnabled { get; init; } = true;
}
