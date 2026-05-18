namespace Migration.Infrastructure.State.OperationalStore.Sql;

public sealed class SqlOperationalStoreOptions
{
    public const string SectionName = "OperationalStore:Sql";

    public string ConnectionStringName { get; set; } = "MigrationOperationalStore";

    public string? ConnectionString { get; set; }

    public string SchemaName { get; set; } = "migration";

    public int CommandTimeoutSeconds { get; set; } = 30;
}
