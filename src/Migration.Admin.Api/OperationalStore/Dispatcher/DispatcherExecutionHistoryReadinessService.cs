using Migration.Infrastructure.State.OperationalStore.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryReadinessService
    : IDispatcherExecutionHistoryReadinessService
{
    private static readonly string[] RequiredColumns =
    {
        "ExecutionId",
        "WorkerId",
        "StartedAt",
        "CompletedAt",
        "DurationMilliseconds",
        "RequestedLeaseCount",
        "LeasedCount",
        "CompletedCount",
        "FailedCount",
        "Outcome",
        "Message"
    };

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;

    public DispatcherExecutionHistoryReadinessService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
    }

    public async Task<DispatcherExecutionHistoryReadinessResponse> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();
        var messages = new List<string>();
        var missingColumns = new List<string>();

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var tableExists = await TableExistsAsync(
            connection,
            schema,
            cancellationToken);

        if (!tableExists)
        {
            messages.Add($"Table [{schema}].[DispatcherExecutions] does not exist.");

            return new DispatcherExecutionHistoryReadinessResponse
            {
                Ready = false,
                ServiceRegistered = true,
                TableExists = false,
                RequiredColumnsExist = false,
                SchemaName = schema,
                MissingColumns = RequiredColumns,
                Messages = messages
            };
        }

        var existingColumns = await GetColumnsAsync(
            connection,
            schema,
            cancellationToken);

        foreach (var column in RequiredColumns)
        {
            if (!existingColumns.Contains(column, StringComparer.OrdinalIgnoreCase))
            {
                missingColumns.Add(column);
            }
        }

        var requiredColumnsExist = missingColumns.Count == 0;

        if (requiredColumnsExist)
        {
            messages.Add("Dispatcher execution history table is ready.");
        }
        else
        {
            messages.Add("Dispatcher execution history table is missing required columns.");
        }

        return new DispatcherExecutionHistoryReadinessResponse
        {
            Ready = tableExists && requiredColumnsExist,
            ServiceRegistered = true,
            TableExists = tableExists,
            RequiredColumnsExist = requiredColumnsExist,
            SchemaName = schema,
            MissingColumns = missingColumns,
            Messages = messages
        };
    }

    private static async Task<bool> TableExistsAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @SchemaName
              AND TABLE_NAME = N'DispatcherExecutions';
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SchemaName", schema);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<HashSet<string>> GetColumnsAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @SchemaName
              AND TABLE_NAME = N'DispatcherExecutions';
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SchemaName", schema);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }
}
