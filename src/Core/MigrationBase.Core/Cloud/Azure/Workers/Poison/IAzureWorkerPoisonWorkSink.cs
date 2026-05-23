namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Persists poison/abandonment records to the configured operational store.
/// </summary>
public interface IAzureWorkerPoisonWorkSink
{
    Task RecordAsync(AzureWorkerPoisonWorkRecord record, CancellationToken cancellationToken);
}
