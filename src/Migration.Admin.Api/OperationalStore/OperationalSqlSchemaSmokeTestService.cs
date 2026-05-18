using Migration.Infrastructure.State.OperationalStore.Sql;
using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalSqlSchemaSmokeTestService
    : IOperationalSqlSchemaSmokeTestService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public OperationalSqlSchemaSmokeTestService(
        ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<OperationalSqlSchemaSmokeTestResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();

        try
        {
            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            var runsExists = await TableExistsAsync(connection, "MigrationRuns", cancellationToken);
            var manifestExists = await TableExistsAsync(connection, "MigrationManifestRecords", cancellationToken);
            var workItemsExists = await TableExistsAsync(connection, "MigrationWorkItems", cancellationToken);
            var failuresExists = await TableExistsAsync(connection, "MigrationFailures", cancellationToken);
            var checkpointsExists = await TableExistsAsync(connection, "MigrationCheckpoints", cancellationToken);
            var identifierMapsExists = await TableExistsAsync(connection, "MigrationIdentifierMaps", cancellationToken);

            if (!runsExists)
            {
                messages.Add("MigrationRuns table missing.");
            }

            if (!manifestExists)
            {
                messages.Add("MigrationManifestRecords table missing.");
            }

            if (!workItemsExists)
            {
                messages.Add("MigrationWorkItems table missing.");
            }

            if (!failuresExists)
            {
                messages.Add("MigrationFailures table missing.");
            }

            if (!checkpointsExists)
            {
                messages.Add("MigrationCheckpoints table missing.");
            }

            if (!identifierMapsExists)
            {
                messages.Add("MigrationIdentifierMaps table missing.");
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
                messages.Add("Operational SQL schema smoke test passed.");
            }

            return new OperationalSqlSchemaSmokeTestResult
            {
                Success = success,
                ConnectionSucceeded = true,
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
                Messages = messages
            };
        }
    }

    private static async Task<bool> TableExistsAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_SCHEMA = 'dbo'
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
            "@TableName",
            tableName);

        var result = await command.ExecuteScalarAsync(
            cancellationToken);

        return result is bool value && value;
    }
}
