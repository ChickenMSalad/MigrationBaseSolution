namespace MigrationBase.Core.Cloud.Azure.Integration.Sql;

public sealed class AzureSqlOperationalStoreRegistry : IAzureSqlOperationalStoreRegistry
{
    private readonly IReadOnlyCollection<AzureSqlOperationalStoreDescriptor> _stores;

    public AzureSqlOperationalStoreRegistry(IEnumerable<AzureSqlOperationalStoreDescriptor>? stores = null)
    {
        _stores = (stores ?? CreateDefaultStores()).ToArray();
    }

    public IReadOnlyCollection<AzureSqlOperationalStoreDescriptor> GetStores() => _stores;

    public AzureSqlOperationalStoreDescriptor? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _stores.FirstOrDefault(store => string.Equals(store.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public AzureSqlOperationalStoreValidationResult Validate()
    {
        var result = new AzureSqlOperationalStoreValidationResult();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var store in _stores)
        {
            if (string.IsNullOrWhiteSpace(store.Name)) result.AddError("SQL operational store name is required.");
            if (string.IsNullOrWhiteSpace(store.ConfigurationKey)) result.AddError($"SQL operational store '{store.Name}' must declare ConfigurationKey.");
            if (!string.IsNullOrWhiteSpace(store.Name) && !names.Add(store.Name)) result.AddError($"Duplicate SQL operational store name '{store.Name}'.");
            if (!string.IsNullOrWhiteSpace(store.ConfigurationKey) && !keys.Add(store.ConfigurationKey)) result.AddWarning($"SQL configuration key '{store.ConfigurationKey}' is used by multiple stores.");
            if (store.RequiresManagedIdentity && store.AllowsConnectionStringFallback) result.AddWarning($"SQL operational store '{store.Name}' allows connection string fallback while managed identity is required.");
        }

        return result;
    }

    private static IEnumerable<AzureSqlOperationalStoreDescriptor> CreateDefaultStores()
    {
        yield return new AzureSqlOperationalStoreDescriptor
        {
            Name = "operations",
            ConfigurationKey = "ConnectionStrings:MigrationOperationsSql",
            Purpose = "Primary durable operational store for runs, work items, approvals, history, replay governance, and runtime state."
        };

        yield return new AzureSqlOperationalStoreDescriptor
        {
            Name = "manifests",
            ConfigurationKey = "ConnectionStrings:MigrationManifestSql",
            Purpose = "Large manifest and source/target mapping store used during real migration execution."
        };

        yield return new AzureSqlOperationalStoreDescriptor
        {
            Name = "audit",
            ConfigurationKey = "ConnectionStrings:MigrationAuditSql",
            Purpose = "Execution audit, validation evidence, and operational readiness evidence store."
        };
    }
}
