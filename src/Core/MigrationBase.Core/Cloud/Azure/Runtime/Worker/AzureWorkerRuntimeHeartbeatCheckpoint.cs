namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker;

public sealed class AzureWorkerRuntimeHeartbeatCheckpoint
{
    public required string WorkerId { get; init; }

    public required string HostRole { get; init; }

    public string? EnvironmentName { get; init; }

    public string? ExecutionRunId { get; init; }

    public string? WorkItemId { get; init; }

    public AzureWorkerHeartbeatCheckpointStatus Status { get; init; } = AzureWorkerHeartbeatCheckpointStatus.Unknown;

    public DateTimeOffset ObservedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LeaseExpiresAtUtc { get; init; }

    public string? StatusReason { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
