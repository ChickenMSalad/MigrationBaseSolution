namespace MigrationBase.Core.Cloud.Azure.Workers;

public interface IAzureWorkerLifecycleReporter
{
    Task ReportAsync(
        AzureWorkerLifecycleState state,
        CancellationToken cancellationToken = default);
}
