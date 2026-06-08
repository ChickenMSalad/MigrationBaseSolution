using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalSqlSchemaSmokeTestService
    : IOperationalSqlSchemaSmokeTestService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalSqlSchemaSmokeTestService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<OperationalSqlSchemaSmokeTestResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var schemaName = string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;

        try
        {
            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            var runsExists = await TableExistsAsync(connection, schemaName, "MigrationRuns", cancellationToken);
            var manifestExists = await TableExistsAsync(connection, schemaName, "MigrationManifestRecords", cancellationToken);
            var workItemsExists = await TableExistsAsync(connection, schemaName, "MigrationWorkItems", cancellationToken);
            var failuresExists = await TableExistsAsync(connection, schemaName, "MigrationFailures", cancellationToken);
            var checkpointsExists = await TableExistsAsync(connection, schemaName, "MigrationCheckpoints", cancellationToken);
            var identifierMapsExists = await TableExistsAsync(connection, schemaName, "MigrationIdentifierMaps", cancellationToken);

            if (!runsExists)
            {
                messages.Add($"{schemaName}.MigrationRuns table missing.");
            }

            if (!manifestExists)
            {
                messages.Add($"{schemaName}.MigrationManifestRecords table missing.");
            }

            if (!workItemsExists)
            {
                messages.Add($"{schemaName}.MigrationWorkItems table missing.");
            }

            if (!failuresExists)
            {
                messages.Add($"{schemaName}.MigrationFailures table missing.");
            }

            if (!checkpointsExists)
            {
                messages.Add($"{schemaName}.MigrationCheckpoints table missing.");
            }

            if (!identifierMapsExists)
            {
                messages.Add($"{schemaName}.MigrationIdentifierMaps table missing.");
            }

            var success =
                runsExists &&
                manifestExists &&
                workItemsExists &&
                failuresExists &&
                checkpointsExists &&
                identifierMapsExists;

            if (success)
            {
                messages.Add($"Operational SQL schema smoke test passed for schema '{schemaName}'.");
            }

            return new OperationalSqlSchemaSmokeTestResult
            {
                Success = success,
                ConnectionSucceeded = true,
                SchemaName = schemaName,
                RunsTableExists = runsExists,
                ManifestTableExists = manifestExists,
                WorkItemsTableExists = workItemsExists,
                FailuresTableExists = failuresExists,
                CheckpointsTableExists = checkpointsExists,
                IdentifierMapsTableExists = identifierMapsExists,
                Messages = messages
            };
        }
        catch (Exception ex)
        {
            messages.Add(ex.Message);

            return new OperationalSqlSchemaSmokeTestResult
            {
                Success = false,
                ConnectionSucceeded = false,
                SchemaName = schemaName,
                Messages = messages
            };
        }
    }

    private static async Task<bool> TableExistsAsync(
        SqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = @SchemaName
                      AND TABLE_NAME = @TableName
                )
                THEN CAST(1 AS BIT)
                ELSE CAST(0 AS BIT)
            END
            """;

        await using var command = new SqlCommand(
            sql,
            connection);

        command.Parameters.AddWithValue(
            "@SchemaName",
            schemaName);

        command.Parameters.AddWithValue(
            "@TableName",
            tableName);

        var result = await command.ExecuteScalarAsync(
            cancellationToken);

        return result is bool value && value;
    }
}
