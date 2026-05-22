namespace Migration.Infrastructure.Sql.Operational.Leases;

public sealed class SqlOperationalWorkItemLeaseCoordinatorOptions
{
    public string? ConnectionString { get; set; }

    public string ConnectionStringName { get; set; } = "MigrationOperationalStore";

    public string SchemaName { get; set; } = "migration";

    public string WorkItemsTableName { get; set; } = "OperationalWorkItems";

    public int DefaultLeaseSeconds { get; set; } = 300;

    public int MaxLeaseSeconds { get; set; } = 3600;

    public int MaxExpiredLeaseReleaseBatchSize { get; set; } = 500;
}
