namespace MigrationBase.Core.Cloud.Azure.Workers.Stabilization;

/// <summary>
/// Summarizes the stabilization contract surface for a worker role/environment pair.
/// </summary>
public sealed class AzureWorkerStabilizationReadinessReport
{
    public AzureWorkerStabilizationReadinessReport(
        string environmentName,
        string workerRole,
        IReadOnlyCollection<AzureWorkerStabilizationChecklistItem> items)
    {
        EnvironmentName = environmentName ?? string.Empty;
        WorkerRole = workerRole ?? string.Empty;
        Items = items ?? Array.Empty<AzureWorkerStabilizationChecklistItem>();
    }

    public string EnvironmentName { get; }

    public string WorkerRole { get; }

    public IReadOnlyCollection<AzureWorkerStabilizationChecklistItem> Items { get; }

    public bool IsReadyForDeploymentAutomation => Items.Count > 0 && Items.All(item => item.IsReady);

    public IReadOnlyCollection<AzureWorkerStabilizationChecklistItem> BlockingItems =>
        Items.Where(item => !item.IsReady).ToArray();
}
