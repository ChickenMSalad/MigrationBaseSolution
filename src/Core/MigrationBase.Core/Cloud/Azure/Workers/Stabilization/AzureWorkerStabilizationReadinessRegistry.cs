namespace MigrationBase.Core.Cloud.Azure.Workers.Stabilization;

/// <summary>
/// Default SDK-free registry for the P5.2 worker stabilization closeout checklist.
/// </summary>
public sealed class AzureWorkerStabilizationReadinessRegistry : IAzureWorkerStabilizationReadinessRegistry
{
    private static readonly AzureWorkerStabilizationChecklistItem[] DefaultItems =
    {
        new("worker.lifecycle", "Worker lifecycle contracts", AzureWorkerStabilizationReadinessStatus.Defined, "platform", "P5.2.1", "Startup, running, degraded, drain, and stopped states are modeled."),
        new("worker.heartbeat", "Heartbeat contracts", AzureWorkerStabilizationReadinessStatus.Defined, "platform", "P5.2.2", "Worker heartbeat identity, cadence, and freshness contracts are modeled."),
        new("worker.shutdown", "Drain and shutdown contracts", AzureWorkerStabilizationReadinessStatus.Defined, "platform", "P5.2.3", "Shutdown intent, drain windows, and terminal outcomes are modeled."),
        new("worker.lease", "Execution lease contracts", AzureWorkerStabilizationReadinessStatus.Defined, "platform", "P5.2.4", "Lease ownership, renewal, expiration, and transfer contracts are modeled."),
        new("worker.retry", "Retry policy contracts", AzureWorkerStabilizationReadinessStatus.Defined, "platform", "P5.2.5", "Retry classification, backoff, and terminal retry behavior are modeled."),
        new("worker.poison", "Poison work contracts", AzureWorkerStabilizationReadinessStatus.Defined, "platform", "P5.2.6", "Poison work classification, evidence, and quarantine expectations are modeled."),
        new("worker.concurrency", "Concurrency control contracts", AzureWorkerStabilizationReadinessStatus.Defined, "platform", "P5.2.7", "Worker, tenant, run, and source-system concurrency scopes are modeled."),
        new("worker.abandonment", "Abandonment contracts", AzureWorkerStabilizationReadinessStatus.Defined, "platform", "P5.2.8", "Abandonment, recovery, and ownership-loss semantics are modeled."),
        new("worker.circuitBreaker", "Circuit breaker contracts", AzureWorkerStabilizationReadinessStatus.Defined, "platform", "P5.2.9", "Open, half-open, recovery, and operator visibility semantics are modeled."),
        new("worker.diagnostics", "Diagnostics snapshot contracts", AzureWorkerStabilizationReadinessStatus.Defined, "platform", "P5.2.10", "Worker diagnostic snapshots are modeled for later observability work.")
    };

    public AzureWorkerStabilizationReadinessReport CreateReport(string environmentName, string workerRole)
    {
        return new AzureWorkerStabilizationReadinessReport(environmentName, workerRole, DefaultItems);
    }
}
