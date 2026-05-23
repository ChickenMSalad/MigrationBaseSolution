namespace MigrationBase.Core.Cloud.Azure.Infrastructure.Storage;

public sealed class AzureStorageBindingRegistry : IAzureStorageBindingRegistry
{
    private readonly List<AzureStorageAccountBinding> _storageAccounts = new();
    private readonly List<AzureStorageContainerBinding> _containers = new();

    public IReadOnlyCollection<AzureStorageAccountBinding> StorageAccounts => _storageAccounts;
    public IReadOnlyCollection<AzureStorageContainerBinding> Containers => _containers;

    public AzureStorageBindingRegistry AddStorageAccount(AzureStorageAccountBinding binding)
    {
        if (binding is null) throw new ArgumentNullException(nameof(binding));
        if (_storageAccounts.Any(existing => string.Equals(existing.Name, binding.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Storage account binding already registered: {binding.Name}");
        }
        _storageAccounts.Add(binding);
        return this;
    }

    public AzureStorageBindingRegistry AddContainer(AzureStorageContainerBinding binding)
    {
        if (binding is null) throw new ArgumentNullException(nameof(binding));
        if (_containers.Any(existing => string.Equals(existing.Name, binding.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Storage container binding already registered: {binding.Name}");
        }
        _containers.Add(binding);
        return this;
    }

    public AzureStorageAccountBinding? FindStorageAccount(string name) =>
        _storageAccounts.FirstOrDefault(binding => string.Equals(binding.Name, name, StringComparison.OrdinalIgnoreCase));

    public AzureStorageContainerBinding? FindContainer(string name) =>
        _containers.FirstOrDefault(binding => string.Equals(binding.Name, name, StringComparison.OrdinalIgnoreCase));
}
