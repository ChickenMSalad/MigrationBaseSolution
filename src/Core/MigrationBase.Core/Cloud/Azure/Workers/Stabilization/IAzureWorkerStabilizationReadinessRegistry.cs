namespace MigrationBase.Core.Cloud.Azure.Workers.Stabilization;

/// <summary>
/// Provides the expected worker-stabilization readiness checklist without depending on any Azure SDK.
/// </summary>
public interface IAzureWorkerStabilizationReadinessRegistry
{
    AzureWorkerStabilizationReadinessReport CreateReport(string environmentName, string workerRole);
}
