namespace Migration.Infrastructure.Sql.Options;

public sealed class SqlOperationalStoreOptions
{
    public const string SectionName = "Migration:OperationalStore:Sql";

    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 60;

    public int WorkItemLeaseMinutes { get; set; } = 15;
}
