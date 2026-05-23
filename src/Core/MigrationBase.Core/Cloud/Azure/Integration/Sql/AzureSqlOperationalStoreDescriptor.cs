namespace MigrationBase.Core.Cloud.Azure.Integration.Sql;

public sealed class AzureSqlOperationalStoreDescriptor
{
    public required string Name { get; init; }
    public required string ConfigurationKey { get; init; }
    public string Purpose { get; init; } = string.Empty;
    public bool IsRequired { get; init; } = true;
    public bool RequiresManagedIdentity { get; init; } = true;
    public bool AllowsConnectionStringFallback { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
