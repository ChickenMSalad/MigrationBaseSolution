using Migration.Infrastructure.State.OperationalStore.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMetricsService : IOperationalMetricsService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _sqlOptions;
    private readonly IOptions<OperationalLeaseExpirationOptions> _leaseOptions;

    public OperationalMetricsService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> sqlOptions,
        IOptions<OperationalLeaseExpirationOptions> leaseOptions)
    {
        _connectionFactory = connectionFactory;
        _sqlOptions = sqlOptions;
        _leaseOptions = leaseOptions;
    }

    public async Task<OperationalWorkItemMetricsResponse> GetWorkItemMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();
        var expiresBefore = DateTimeOffset.UtcNow.AddMinutes(-GetLeaseTimeoutMinutes());

        var sql = $"""
            SELECT
                TotalCount = COUNT(1),
                CreatedCount = SUM(CASE WHEN Status = N'Created' THEN 1 ELSE 0 END),
                LockedCount = SUM(CASE WHEN Status = N'Locked' THEN 1 ELSE 0 END),
                ProcessingCount = SUM(CASE WHEN Status = N'Processing' THEN 1 ELSE 0 END),
                CompletedCount = SUM(CASE WHEN Status = N'Completed' THEN 1 ELSE 0 END),
                FailedCount = SUM(CASE WHEN Status = N'Failed' THEN 1 ELSE 0 END),
                AverageAttemptCount = COALESCE(AVG(CAST(AttemptCount AS DECIMAL(18,2))), 0),
                OldestCreatedAt = MIN(CASE WHEN Status = N'Created' THEN CreatedAt END),
                OldestLockedAt = MIN(CASE WHEN Status = N'Locked' THEN LockedAt END),
                ExpiredLeaseCount = SUM(CASE
                    WHEN Status = N'Locked'
                     AND LockedAt IS NOT NULL
                     AND LockedAt < @ExpiresBefore
                     AND CompletedAt IS NULL
                     AND FailedAt IS NULL
                    THEN 1 ELSE 0 END)
            FROM [{schema}].[MigrationWorkItems];
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ExpiresBefore", expiresBefore);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new OperationalWorkItemMetricsResponse();
        }

        return new OperationalWorkItemMetricsResponse
        {
            TotalCount = ReadInt32(reader, "TotalCount"),
            CreatedCount = ReadInt32(reader, "CreatedCount"),
            LockedCount = ReadInt32(reader, "LockedCount"),
            ProcessingCount = ReadInt32(reader, "ProcessingCount"),
            CompletedCount = ReadInt32(reader, "CompletedCount"),
            FailedCount = ReadInt32(reader, "FailedCount"),
            AverageAttemptCount = ReadDecimal(reader, "AverageAttemptCount"),
            OldestCreatedAt = ReadNullableDateTimeOffset(reader, "OldestCreatedAt"),
            OldestLockedAt = ReadNullableDateTimeOffset(reader, "OldestLockedAt"),
            ExpiredLeaseCount = ReadInt32(reader, "ExpiredLeaseCount")
        };
    }

    public async Task<OperationalLeaseMetricsResponse> GetLeaseMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();
        var leaseTimeoutMinutes = GetLeaseTimeoutMinutes();
        var expiresBefore = DateTimeOffset.UtcNow.AddMinutes(-leaseTimeoutMinutes);

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var summarySql = $"""
            SELECT
                LockedCount = COUNT(1),
                ExpiredCount = SUM(CASE
                    WHEN LockedAt IS NOT NULL
                     AND LockedAt < @ExpiresBefore
                     AND CompletedAt IS NULL
                     AND FailedAt IS NULL
                    THEN 1 ELSE 0 END),
                OldestLockedAt = MIN(LockedAt),
                OldestLockedBy = (
                    SELECT TOP (1) LockedBy
                    FROM [{schema}].[MigrationWorkItems]
                    WHERE Status = N'Locked'
                      AND LockedBy IS NOT NULL
                    ORDER BY LockedAt
                ),
                DistinctWorkerCount = COUNT(DISTINCT LockedBy)
            FROM [{schema}].[MigrationWorkItems]
            WHERE Status = N'Locked';
            """;

        await using var summaryCommand = new SqlCommand(summarySql, connection);
        summaryCommand.Parameters.AddWithValue("@ExpiresBefore", expiresBefore);

        int lockedCount;
        int expiredCount;
        DateTimeOffset? oldestLockedAt;
        string? oldestLockedBy;
        int distinctWorkerCount;

        await using (var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                lockedCount = 0;
                expiredCount = 0;
                oldestLockedAt = null;
                oldestLockedBy = null;
                distinctWorkerCount = 0;
            }
            else
            {
                lockedCount = ReadInt32(reader, "LockedCount");
                expiredCount = ReadInt32(reader, "ExpiredCount");
                oldestLockedAt = ReadNullableDateTimeOffset(reader, "OldestLockedAt");
                oldestLockedBy = ReadNullableString(reader, "OldestLockedBy");
                distinctWorkerCount = ReadInt32(reader, "DistinctWorkerCount");
            }
        }

        var workers = await ReadWorkerMetricsAsync(
            connection,
            schema,
            cancellationToken);

        return new OperationalLeaseMetricsResponse
        {
            LeaseTimeoutMinutes = leaseTimeoutMinutes,
            LockedCount = lockedCount,
            ExpiredCount = expiredCount,
            OldestLockedAt = oldestLockedAt,
            OldestLockedBy = oldestLockedBy,
            DistinctWorkerCount = distinctWorkerCount,
            Workers = workers
        };
    }

    public async Task<OperationalRunMetricsResponse> GetRunMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var summarySql = $"""
            SELECT
                TotalCount = COUNT(1),
                CreatedCount = SUM(CASE WHEN Status = N'Created' THEN 1 ELSE 0 END),
                StartedCount = SUM(CASE WHEN Status = N'Started' THEN 1 ELSE 0 END),
                CompletedCount = SUM(CASE WHEN Status = N'Completed' THEN 1 ELSE 0 END),
                FailedCount = SUM(CASE WHEN Status = N'Failed' THEN 1 ELSE 0 END),
                OldestCreatedAt = MIN(CreatedAt),
                NewestCreatedAt = MAX(CreatedAt)
            FROM [{schema}].[MigrationRuns];
            """;

        await using var summaryCommand = new SqlCommand(summarySql, connection);

        int totalCount;
        int createdCount;
        int startedCount;
        int completedCount;
        int failedCount;
        DateTimeOffset? oldestCreatedAt;
        DateTimeOffset? newestCreatedAt;

        await using (var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                totalCount = 0;
                createdCount = 0;
                startedCount = 0;
                completedCount = 0;
                failedCount = 0;
                oldestCreatedAt = null;
                newestCreatedAt = null;
            }
            else
            {
                totalCount = ReadInt32(reader, "TotalCount");
                createdCount = ReadInt32(reader, "CreatedCount");
                startedCount = ReadInt32(reader, "StartedCount");
                completedCount = ReadInt32(reader, "CompletedCount");
                failedCount = ReadInt32(reader, "FailedCount");
                oldestCreatedAt = ReadNullableDateTimeOffset(reader, "OldestCreatedAt");
                newestCreatedAt = ReadNullableDateTimeOffset(reader, "NewestCreatedAt");
            }
        }

        var statuses = await ReadRunStatusMetricsAsync(
            connection,
            schema,
            cancellationToken);

        return new OperationalRunMetricsResponse
        {
            TotalCount = totalCount,
            CreatedCount = createdCount,
            StartedCount = startedCount,
            CompletedCount = completedCount,
            FailedCount = failedCount,
            OldestCreatedAt = oldestCreatedAt,
            NewestCreatedAt = newestCreatedAt,
            Statuses = statuses
        };
    }

    public async Task<OperationalDiagnosticsSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        return new OperationalDiagnosticsSummaryResponse
        {
            Runs = await GetRunMetricsAsync(cancellationToken),
            WorkItems = await GetWorkItemMetricsAsync(cancellationToken),
            Leases = await GetLeaseMetricsAsync(cancellationToken),
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<IReadOnlyCollection<OperationalLeaseWorkerMetric>> ReadWorkerMetricsAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                WorkerId = LockedBy,
                LockedCount = COUNT(1),
                OldestLockedAt = MIN(LockedAt),
                NewestLockedAt = MAX(LockedAt)
            FROM [{schema}].[MigrationWorkItems]
            WHERE Status = N'Locked'
              AND LockedBy IS NOT NULL
            GROUP BY LockedBy
            ORDER BY LockedCount DESC, WorkerId;
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var workers = new List<OperationalLeaseWorkerMetric>();

        while (await reader.ReadAsync(cancellationToken))
        {
            workers.Add(new OperationalLeaseWorkerMetric
            {
                WorkerId = reader.GetString(reader.GetOrdinal("WorkerId")),
                LockedCount = ReadInt32(reader, "LockedCount"),
                OldestLockedAt = ReadNullableDateTimeOffset(reader, "OldestLockedAt"),
                NewestLockedAt = ReadNullableDateTimeOffset(reader, "NewestLockedAt")
            });
        }

        return workers;
    }

    private async Task<IReadOnlyCollection<OperationalRunStatusMetric>> ReadRunStatusMetricsAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                Status,
                Count = COUNT(1)
            FROM [{schema}].[MigrationRuns]
            GROUP BY Status
            ORDER BY Status;
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var statuses = new List<OperationalRunStatusMetric>();

        while (await reader.ReadAsync(cancellationToken))
        {
            statuses.Add(new OperationalRunStatusMetric
            {
                Status = reader.GetString(reader.GetOrdinal("Status")),
                Count = ReadInt32(reader, "Count")
            });
        }

        return statuses;
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_sqlOptions.Value.SchemaName)
            ? "migration"
            : _sqlOptions.Value.SchemaName;
    }

    private int GetLeaseTimeoutMinutes()
    {
        return Math.Max(1, _leaseOptions.Value.LeaseTimeoutMinutes);
    }

    private static int ReadInt32(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static decimal ReadDecimal(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
        {
            return 0m;
        }

        return Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static string? ReadNullableString(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
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
