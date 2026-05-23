namespace MigrationBase.Core.Cloud.Azure.Runtime.Workers.Closeout;

/// <summary>
/// Describes the completion status for the P6.3 worker runtime foundation slice.
/// This type intentionally contains no host wiring or Azure SDK dependency.
/// </summary>
public sealed class AzureWorkerRuntimeFoundationCloseout
{
    public string Phase { get; init; } = "P6.3";

    public string Name { get; init; } = "Worker Runtime Foundation";

    public bool RuntimeLoopDefined { get; init; } = true;

    public bool HeartbeatCheckpointDefined { get; init; } = true;

    public bool LeaseCoordinationDefined { get; init; } = true;

    public bool RetryOutcomeDefined { get; init; } = true;

    public bool PoisonAbandonmentDefined { get; init; } = true;

    public bool CapacityGateDefined { get; init; } = true;

    public IReadOnlyList<string> HandoffTargets { get; init; } = new[]
    {
        "P6.4 Queue and dispatcher mechanics",
        "P6.5 Manifest execution runtime",
        "P6.6 Failure, retry, and replay runtime integration"
    };
}
