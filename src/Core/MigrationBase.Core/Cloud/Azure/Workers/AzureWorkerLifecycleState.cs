namespace MigrationBase.Core.Cloud.Azure.Workers;

public sealed record AzureWorkerLifecycleState
{
    public string WorkerId { get; init; } = string.Empty;

    public string HostRole { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public AzureWorkerLifecyclePhase Phase { get; init; } = AzureWorkerLifecyclePhase.Unknown;

    public DateTimeOffset ObservedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? Reason { get; init; }

    public bool IsTerminal =>
        Phase == AzureWorkerLifecyclePhase.Stopped ||
        Phase == AzureWorkerLifecyclePhase.Faulted;
}
