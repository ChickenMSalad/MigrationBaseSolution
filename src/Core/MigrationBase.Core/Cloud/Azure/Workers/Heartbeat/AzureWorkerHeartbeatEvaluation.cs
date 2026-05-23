namespace MigrationBase.Core.Cloud.Azure.Workers.Heartbeat;

public sealed class AzureWorkerHeartbeatEvaluation
{
    public string WorkerId { get; set; } = string.Empty;

    public AzureWorkerHeartbeatState State { get; set; } = AzureWorkerHeartbeatState.Unknown;

    public TimeSpan Age { get; set; } = TimeSpan.Zero;

    public bool IsHealthy { get; set; }

    public bool RequiresOperatorAttention { get; set; }

    public string Reason { get; set; } = string.Empty;

    public static AzureWorkerHeartbeatEvaluation From(
        AzureWorkerHeartbeatDescriptor descriptor,
        AzureWorkerHeartbeatOptions options,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(options);

        var age = nowUtc - descriptor.ObservedAtUtc;
        var state = descriptor.State;
        var reason = "Heartbeat state reported by worker.";

        if (age.TotalSeconds >= options.LostAfterSeconds)
        {
            state = AzureWorkerHeartbeatState.Lost;
            reason = "Heartbeat exceeded lost threshold.";
        }
        else if (age.TotalSeconds >= options.StaleAfterSeconds)
        {
            state = AzureWorkerHeartbeatState.Stale;
            reason = "Heartbeat exceeded stale threshold.";
        }

        var healthy = state == AzureWorkerHeartbeatState.Healthy || state == AzureWorkerHeartbeatState.Draining;

        return new AzureWorkerHeartbeatEvaluation
        {
            WorkerId = descriptor.WorkerId,
            State = state,
            Age = age < TimeSpan.Zero ? TimeSpan.Zero : age,
            IsHealthy = healthy,
            RequiresOperatorAttention = state is AzureWorkerHeartbeatState.Stale or AzureWorkerHeartbeatState.Lost or AzureWorkerHeartbeatState.Faulted,
            Reason = reason
        };
    }
}
