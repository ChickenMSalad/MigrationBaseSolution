namespace MigrationBase.Core.Cloud.Azure;

public sealed class AzureSqlOperationalStoreOptions
{
    public string ConnectionStringName { get; set; } = "MigrationBaseOperationalSql";

    public string ServerName { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public bool RequireEncryptedConnection { get; set; } = true;

    public bool RequireManagedIdentityInCloud { get; set; } = true;

    public int CommandTimeoutSeconds { get; set; } = 60;

    public int LongRunningCommandTimeoutSeconds { get; set; } = 600;
}
