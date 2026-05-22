namespace Migration.Infrastructure.State.OperationalStore.Sql.Queries;

internal static class OperationalStoreValidationSql
{
    public const string ConnectivityProbe = "SELECT 1;";

    public const string RequiredTablesExist = """
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
AND TABLE_NAME IN
(
    'MigrationRuns',
    'MigrationManifestRecords',
    'MigrationWorkItems',
    'MigrationFailures',
    'MigrationCheckpoints',
    'MigrationIdentifierMaps'
);
""";
}
