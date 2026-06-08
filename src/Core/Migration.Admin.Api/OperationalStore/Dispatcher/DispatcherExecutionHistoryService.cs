using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryService
    : IDispatcherExecutionHistoryService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;

    public DispatcherExecutionHistoryService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
    }

    public async Task RecordAsync(
        DispatcherExecutionRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var schema = GetSchemaName();

        var sql = $"""
            INSERT INTO [{schema}].[DispatcherExecutions]
            (
                ExecutionId,
                WorkerId,
                StartedAt,
                CompletedAt,
                DurationMilliseconds,
                RequestedLeaseCount,
                LeasedCount,
                CompletedCount,
                FailedCount,
                Outcome,
                Message
            )
            VALUES
            (
                @ExecutionId,
                @WorkerId,
                @StartedAt,
                @CompletedAt,
                @DurationMilliseconds,
                @RequestedLeaseCount,
                @LeasedCount,
                @CompletedCount,
                @FailedCount,
                @Outcome,
                @Message
            );
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@ExecutionId", record.ExecutionId);
        command.Parameters.AddWithValue("@WorkerId", record.WorkerId);
        command.Parameters.AddWithValue("@StartedAt", record.StartedAt);
        command.Parameters.AddWithValue("@CompletedAt", record.CompletedAt);
        command.Parameters.AddWithValue("@DurationMilliseconds", record.DurationMilliseconds);
        command.Parameters.AddWithValue("@RequestedLeaseCount", record.RequestedLeaseCount);
        command.Parameters.AddWithValue("@LeasedCount", record.LeasedCount);
        command.Parameters.AddWithValue("@CompletedCount", record.CompletedCount);
        command.Parameters.AddWithValue("@FailedCount", record.FailedCount);
        command.Parameters.AddWithValue("@Outcome", record.Outcome);
        command.Parameters.AddWithValue("@Message", record.Message);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DispatcherExecutionRecord>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        var sql = $"""
            SELECT TOP (@Count)
                ExecutionId,
                WorkerId,
                StartedAt,
                CompletedAt,
                DurationMilliseconds,
                RequestedLeaseCount,
                LeasedCount,
                CompletedCount,
                FailedCount,
                Outcome,
                Message
            FROM [{schema}].[DispatcherExecutions]
            ORDER BY StartedAt DESC;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Count", Math.Clamp(count, 1, 250));

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<DispatcherExecutionRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public async Task<DispatcherExecutionRecord?> GetAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        var sql = $"""
            SELECT
                ExecutionId,
                WorkerId,
                StartedAt,
                CompletedAt,
                DurationMilliseconds,
                RequestedLeaseCount,
                LeasedCount,
                CompletedCount,
                FailedCount,
                Outcome,
                Message
            FROM [{schema}].[DispatcherExecutions]
            WHERE ExecutionId = @ExecutionId;
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ExecutionId", executionId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }

    private static DispatcherExecutionRecord Map(SqlDataReader reader)
    {
        return new DispatcherExecutionRecord
        {
            ExecutionId = reader.GetGuid(reader.GetOrdinal("ExecutionId")),
            WorkerId = reader.GetString(reader.GetOrdinal("WorkerId")),
            StartedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("StartedAt")),
            CompletedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CompletedAt")),
            DurationMilliseconds = reader.GetInt64(reader.GetOrdinal("DurationMilliseconds")),
            RequestedLeaseCount = reader.GetInt32(reader.GetOrdinal("RequestedLeaseCount")),
            LeasedCount = reader.GetInt32(reader.GetOrdinal("LeasedCount")),
            CompletedCount = reader.GetInt32(reader.GetOrdinal("CompletedCount")),
            FailedCount = reader.GetInt32(reader.GetOrdinal("FailedCount")),
            Outcome = reader.GetString(reader.GetOrdinal("Outcome")),
            Message = reader.GetString(reader.GetOrdinal("Message"))
        };
    }
}
