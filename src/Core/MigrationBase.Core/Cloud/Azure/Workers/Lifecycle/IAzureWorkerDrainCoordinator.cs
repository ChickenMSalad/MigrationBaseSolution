namespace MigrationBase.Core.Cloud.Azure.Workers.Lifecycle;

/// <summary>
/// Coordinates graceful worker drain behavior without binding the core contracts to a specific host implementation.
/// </summary>
public interface IAzureWorkerDrainCoordinator
{
    Task<AzureWorkerDrainStatus> RequestDrainAsync(AzureWorkerDrainRequest request, CancellationToken cancellationToken = default);

    Task<AzureWorkerDrainStatus> GetDrainStatusAsync(string workerId, CancellationToken cancellationToken = default);

    Task<AzureWorkerDrainStatus> CompleteDrainAsync(string workerId, string message, CancellationToken cancellationToken = default);
}
