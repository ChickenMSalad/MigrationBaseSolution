namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker.Capacity;

public sealed class AzureWorkerCapacityGate : IAzureWorkerCapacityGate
{
    public AzureWorkerCapacityDecision Evaluate(AzureWorkerCapacitySnapshot snapshot, AzureWorkerCapacityLimit limit)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(limit);

        if (!limit.IsEnabled)
        {
            return AzureWorkerCapacityDecision.Accept("capacity.limit.disabled");
        }

        if (limit.MaxConcurrentWorkItems.HasValue && snapshot.ActiveWorkItems >= limit.MaxConcurrentWorkItems.Value)
        {
            return AzureWorkerCapacityDecision.Throttle(
                "capacity.activeWorkItems.limitReached",
                "The worker has reached its active work item capacity.",
                TimeSpan.FromSeconds(15));
        }

        if (limit.MaxConcurrentRuns.HasValue && snapshot.ActiveRuns >= limit.MaxConcurrentRuns.Value)
        {
            return AzureWorkerCapacityDecision.Throttle(
                "capacity.activeRuns.limitReached",
                "The worker has reached its active run capacity.",
                TimeSpan.FromSeconds(30));
        }

        if (limit.MaxQueueDepth.HasValue && snapshot.VisibleQueueDepth > limit.MaxQueueDepth.Value)
        {
            return AzureWorkerCapacityDecision.Throttle(
                "capacity.queueDepth.limitExceeded",
                "The visible queue depth exceeds the configured worker capacity threshold.",
                TimeSpan.FromSeconds(30));
        }

        if (limit.MaxLeaseRenewals.HasValue && snapshot.LeaseRenewalCount > limit.MaxLeaseRenewals.Value)
        {
            return AzureWorkerCapacityDecision.Throttle(
                "capacity.leaseRenewal.limitExceeded",
                "The work item has exceeded the configured lease renewal threshold.",
                TimeSpan.FromSeconds(60));
        }

        return AzureWorkerCapacityDecision.Accept();
    }
}
