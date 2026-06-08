namespace Migration.Infrastructure.Sql.Operational.Runs;

public sealed class SqlOperationalRunCoordinatorOptions
{
    public string? ConnectionString { get; set; }

    public string ConnectionStringName { get; set; } = "MigrationOperationalStore";

    public string SchemaName { get; set; } = "dbo";

    public string RunsTableName { get; set; } = "Runs";

    public string ManifestRowsTableName { get; set; } = "ManifestRows";

    public int DefaultFanOutBatchSize { get; set; } = 500;

    public int MaxFanOutBatchSize { get; set; } = 5000;

    public int CoordinationLeaseSeconds { get; set; } = 600;
}
