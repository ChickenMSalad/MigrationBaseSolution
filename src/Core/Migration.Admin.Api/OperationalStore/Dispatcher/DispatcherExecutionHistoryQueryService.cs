using System.Text;
using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryQueryService
    : IDispatcherExecutionHistoryQueryService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;

    public DispatcherExecutionHistoryQueryService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
    }

    public async Task<IReadOnlyCollection<DispatcherExecutionRecord>> QueryAsync(
        DispatcherExecutionHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        query ??= new DispatcherExecutionHistoryQuery();

        var schema = GetSchemaName();

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder();

        sql.Append($"""
            SELECT TOP (@Limit)
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
            WHERE 1 = 1
            """);

        if (!string.IsNullOrWhiteSpace(query.WorkerId))
        {
            sql.Append(" AND WorkerId = @WorkerId");
        }

        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            sql.Append(" AND Outcome = @Outcome");
        }

        sql.Append(" ORDER BY StartedAt DESC, ExecutionId DESC;");

        await using var command =
            new SqlCommand(sql.ToString(), connection);

        command.Parameters.AddWithValue(
            "@Limit",
            Math.Clamp(query.Limit, 1, 500));

        if (!string.IsNullOrWhiteSpace(query.WorkerId))
        {
            command.Parameters.AddWithValue(
                "@WorkerId",
                query.WorkerId);
        }

        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            command.Parameters.AddWithValue(
                "@Outcome",
                query.Outcome);
        }

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<DispatcherExecutionRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DispatcherExecutionRecord
            {
                ExecutionId = reader.GetGuid(reader.GetOrdinal("ExecutionId")),
                WorkerId = reader.GetString(reader.GetOrdinal("WorkerId")),
                StartedAt = reader.GetFieldValue<DateTimeOffset>(
                    reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.GetFieldValue<DateTimeOffset>(
                    reader.GetOrdinal("CompletedAt")),
                DurationMilliseconds = reader.GetInt64(
                    reader.GetOrdinal("DurationMilliseconds")),
                RequestedLeaseCount = reader.GetInt32(
                    reader.GetOrdinal("RequestedLeaseCount")),
                LeasedCount = reader.GetInt32(
                    reader.GetOrdinal("LeasedCount")),
                CompletedCount = reader.GetInt32(
                    reader.GetOrdinal("CompletedCount")),
                FailedCount = reader.GetInt32(
                    reader.GetOrdinal("FailedCount")),
                Outcome = reader.GetString(reader.GetOrdinal("Outcome")),
                Message = reader.GetString(reader.GetOrdinal("Message"))
            });
        }

        return results;
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }
}
