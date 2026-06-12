namespace Migration.Infrastructure.Sql.Options;

public sealed class SqlOperationalStoreOptions
{
    public const string SectionName = "SqlOperationalStore";

    public string? ConnectionString { get; set; }

    public string SchemaName { get; set; } = "migration";

    public int CommandTimeoutSeconds { get; set; } = 30;

    public int WorkItemLeaseMinutes { get; set; } = 5;
}
