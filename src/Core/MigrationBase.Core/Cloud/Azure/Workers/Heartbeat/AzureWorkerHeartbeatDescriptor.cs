namespace MigrationBase.Core.Cloud.Azure.Workers.Heartbeat;

public sealed class AzureWorkerHeartbeatDescriptor
{
    public string WorkerId { get; set; } = string.Empty;

    public string HostRole { get; set; } = string.Empty;

    public string EnvironmentName { get; set; } = string.Empty;

    public string DeploymentRing { get; set; } = string.Empty;

    public AzureWorkerHeartbeatState State { get; set; } = AzureWorkerHeartbeatState.Unknown;

    public DateTimeOffset ObservedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastSuccessfulWorkItemAtUtc { get; set; }

    public string? CurrentRunId { get; set; }

    public string? CurrentWorkItemId { get; set; }

    public int? ActiveWorkItemCount { get; set; }

    public int? MaxConcurrentWorkItems { get; set; }

    public bool IsDraining { get; set; }

    public string? DrainReason { get; set; }

    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    public bool HasIdentity() =>
        !string.IsNullOrWhiteSpace(WorkerId) &&
        !string.IsNullOrWhiteSpace(HostRole) &&
        !string.IsNullOrWhiteSpace(EnvironmentName);
}
