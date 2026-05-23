namespace MigrationBase.Core.Cloud.Azure.Workers.Concurrency;

public interface IAzureWorkerConcurrencyProfileRegistry
{
    AzureWorkerConcurrencyProfile? FindByName(string profileName);
    AzureWorkerConcurrencyProfile? FindForWorkerRole(string workerRole);
    IReadOnlyCollection<AzureWorkerConcurrencyProfile> ListProfiles();
}
