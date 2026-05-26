namespace Migration.Infrastructure.Sql.Operational.WorkItems;

public sealed class SqlOperationalWorkItemQueueOptions
{
    public const string SectionName = "SqlOperationalWorkItemQueue";

    public string? ConnectionString { get; set; }

    public string? ConnectionStringName { get; set; } = "MigrationOperationalStore";

    public string SchemaName { get; set; } = "migration";

    public string WorkItemsTableName { get; set; } = "WorkItems";

    public int DefaultMaxAttempts { get; set; } = 5;
}
