namespace MigrationBase.Core.Cloud.Azure.Infrastructure.Storage;

public interface IAzureStorageBindingRegistry
{
    IReadOnlyCollection<AzureStorageAccountBinding> StorageAccounts { get; }
    IReadOnlyCollection<AzureStorageContainerBinding> Containers { get; }
    AzureStorageAccountBinding? FindStorageAccount(string name);
    AzureStorageContainerBinding? FindContainer(string name);
}
