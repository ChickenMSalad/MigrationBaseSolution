namespace Migration.Infrastructure.Sql.Operational.ExecutionHistory;

public sealed class SqlOperationalExecutionHistoryOptions
{
    public string ConnectionStringName { get; set; } = "MigrationOperationalStore";

    public string? ConnectionString { get; set; }

    public string SchemaName { get; set; } = "migration";

    public string ExecutionAttemptsTableName { get; set; } = "WorkItemExecutionAttempts";
}
