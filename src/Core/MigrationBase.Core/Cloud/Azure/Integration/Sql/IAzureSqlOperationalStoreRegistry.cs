namespace MigrationBase.Core.Cloud.Azure.Integration.Sql;

public interface IAzureSqlOperationalStoreRegistry
{
    IReadOnlyCollection<AzureSqlOperationalStoreDescriptor> GetStores();
    AzureSqlOperationalStoreDescriptor? FindByName(string name);
    AzureSqlOperationalStoreValidationResult Validate();
}
