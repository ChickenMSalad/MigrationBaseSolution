using Migration.Infrastructure.State.OperationalStore.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class DispatcherExecutionHistoryMetricsService
    : IDispatcherExecutionHistoryMetricsService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;

    public DispatcherExecutionHistoryMetricsService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
    }

    public async Task<DispatcherExecutionHistoryMetricsResponse> GetMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var summary = await ReadSummaryAsync(
            connection,
            schema,
            cancellationToken);

        var workers = await ReadWorkerMetricsAsync(
            connection,
            schema,
            cancellationToken);

        return new DispatcherExecutionHistoryMetricsResponse
        {
            TotalExecutionCount = summary.TotalExecutionCount,
            CompletedExecutionCount = summary.CompletedExecutionCount,
            CompletedWithFailuresExecutionCount = summary.CompletedWithFailuresExecutionCount,
            FailedExecutionCount = summary.FailedExecutionCount,
            TotalLeasedCount = summary.TotalLeasedCount,
            TotalCompletedCount = summary.TotalCompletedCount,
            TotalFailedCount = summary.TotalFailedCount,
            AverageDurationMilliseconds = summary.AverageDurationMilliseconds,
            OldestExecutionStartedAt = summary.OldestExecutionStartedAt,
            NewestExecutionStartedAt = summary.NewestExecutionStartedAt,
            Workers = workers
        };
    }

    private static async Task<DispatcherExecutionHistoryMetricsResponse> ReadSummaryAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                TotalExecutionCount = COUNT(1),
                CompletedExecutionCount = SUM(CASE WHEN Outcome = N'Completed' THEN 1 ELSE 0 END),
                CompletedWithFailuresExecutionCount = SUM(CASE WHEN Outcome = N'CompletedWithFailures' THEN 1 ELSE 0 END),
                FailedExecutionCount = SUM(CASE WHEN Outcome = N'Failed' THEN 1 ELSE 0 END),
                TotalLeasedCount = SUM(LeasedCount),
                TotalCompletedCount = SUM(CompletedCount),
                TotalFailedCount = SUM(FailedCount),
                AverageDurationMilliseconds = COALESCE(AVG(CAST(DurationMilliseconds AS DECIMAL(18, 2))), 0),
                OldestExecutionStartedAt = MIN(StartedAt),
                NewestExecutionStartedAt = MAX(StartedAt)
            FROM [{schema}].[DispatcherExecutions];
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new DispatcherExecutionHistoryMetricsResponse();
        }

        return new DispatcherExecutionHistoryMetricsResponse
        {
            TotalExecutionCount = ReadInt32(reader, "TotalExecutionCount"),
            CompletedExecutionCount = ReadInt32(reader, "CompletedExecutionCount"),
            CompletedWithFailuresExecutionCount = ReadInt32(reader, "CompletedWithFailuresExecutionCount"),
            FailedExecutionCount = ReadInt32(reader, "FailedExecutionCount"),
            TotalLeasedCount = ReadInt32(reader, "TotalLeasedCount"),
            TotalCompletedCount = ReadInt32(reader, "TotalCompletedCount"),
            TotalFailedCount = ReadInt32(reader, "TotalFailedCount"),
            AverageDurationMilliseconds = ReadDecimal(reader, "AverageDurationMilliseconds"),
            OldestExecutionStartedAt = ReadNullableDateTimeOffset(reader, "OldestExecutionStartedAt"),
            NewestExecutionStartedAt = ReadNullableDateTimeOffset(reader, "NewestExecutionStartedAt")
        };
    }

    private static async Task<IReadOnlyCollection<DispatcherWorkerExecutionMetric>> ReadWorkerMetricsAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                WorkerId,
                ExecutionCount = COUNT(1),
                TotalLeasedCount = SUM(LeasedCount),
                TotalCompletedCount = SUM(CompletedCount),
                TotalFailedCount = SUM(FailedCount),
                LastStartedAt = MAX(StartedAt)
            FROM [{schema}].[DispatcherExecutions]
            GROUP BY WorkerId
            ORDER BY LastStartedAt DESC, WorkerId;
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var workers = new List<DispatcherWorkerExecutionMetric>();

        while (await reader.ReadAsync(cancellationToken))
        {
            workers.Add(new DispatcherWorkerExecutionMetric
            {
                WorkerId = reader.GetString(reader.GetOrdinal("WorkerId")),
                ExecutionCount = ReadInt32(reader, "ExecutionCount"),
                TotalLeasedCount = ReadInt32(reader, "TotalLeasedCount"),
                TotalCompletedCount = ReadInt32(reader, "TotalCompletedCount"),
                TotalFailedCount = ReadInt32(reader, "TotalFailedCount"),
                LastStartedAt = ReadNullableDateTimeOffset(reader, "LastStartedAt")
            });
        }

        return workers;
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }

    private static int ReadInt32(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? 0
            : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static decimal ReadDecimal(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? 0m
            : Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }
}
