namespace Migration.Infrastructure.State.OperationalStore.Sql;

public static class SqlOperationalStoreObjectNames
{
    public const string DefaultSchemaName = "migration";

    public const string MigrationRunsTable = "MigrationRuns";

    public const string MigrationManifestRecordsTable = "MigrationManifestRecords";

    public const string MigrationWorkItemsTable = "MigrationWorkItems";

    public const string MigrationIdentifierMapsTable = "MigrationIdentifierMaps";

    public const string MigrationFailuresTable = "MigrationFailures";

    public const string MigrationCheckpointsTable = "MigrationCheckpoints";

    public static string Qualify(string schemaName, string objectName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            throw new ArgumentException("Schema name is required.", nameof(schemaName));
        }

        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new ArgumentException("Object name is required.", nameof(objectName));
        }

        return $"[{schemaName}].[{objectName}]";
    }
}
