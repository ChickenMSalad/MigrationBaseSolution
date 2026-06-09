using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionWorkerHeartbeatStore : IExecutionWorkerHeartbeatStore
{
    private readonly IConfiguration _configuration;

    public SqlExecutionWorkerHeartbeatStore(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task UpsertAsync(
        ExecutionWorkerHeartbeatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WorkerId))
        {
            throw new InvalidOperationException("WorkerId is required.");
        }

        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
MERGE dbo.MigrationExecutionWorkerHeartbeats AS target
USING
(
    SELECT
        @WorkerId AS WorkerId,
        @ExecutionSessionId AS ExecutionSessionId,
        @Status AS Status,
        @ActiveLeaseCount AS ActiveLeaseCount,
        @Message AS Message
) AS source
ON target.WorkerId = source.WorkerId
WHEN MATCHED THEN
    UPDATE SET
        ExecutionSessionId = source.ExecutionSessionId,
        Status = source.Status,
        LastHeartbeatUtc = SYSUTCDATETIME(),
        ActiveLeaseCount = source.ActiveLeaseCount,
        Message = source.Message
WHEN NOT MATCHED THEN
    INSERT
    (
        WorkerId,
        ExecutionSessionId,
        Status,
        LastHeartbeatUtc,
        ActiveLeaseCount,
        Message,
        CreatedUtc
    )
    VALUES
    (
        source.WorkerId,
        source.ExecutionSessionId,
        source.Status,
        SYSUTCDATETIME(),
        source.ActiveLeaseCount,
        source.Message,
        SYSUTCDATETIME()
    );
";

        command.Parameters.AddWithValue("@WorkerId", request.WorkerId.Trim());
        command.Parameters.AddWithValue("@ExecutionSessionId", (object?)request.ExecutionSessionId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", Normalize(request.Status, "unknown"));
        command.Parameters.AddWithValue("@ActiveLeaseCount", Math.Max(0, request.ActiveLeaseCount));
        command.Parameters.AddWithValue("@Message", string.IsNullOrWhiteSpace(request.Message) ? DBNull.Value : request.Message.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ExecutionWorkerTelemetrySummary> ReadSummaryAsync(
        int staleAfterSeconds,
        CancellationToken cancellationToken)
    {
        var safeStaleAfterSeconds = Math.Clamp(staleAfterSeconds, 30, 86400);
        var workers = new List<ExecutionWorkerHeartbeatRecord>();
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    WorkerId,
    ExecutionSessionId,
    Status,
    LastHeartbeatUtc,
    ActiveLeaseCount,
    Message,
    CreatedUtc
FROM dbo.MigrationExecutionWorkerHeartbeats
ORDER BY LastHeartbeatUtc DESC;
";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            workers.Add(new ExecutionWorkerHeartbeatRecord(
                WorkerId: reader.GetString(0),
                ExecutionSessionId: reader.IsDBNull(1) ? null : reader.GetGuid(1),
                Status: reader.GetString(2),
                LastHeartbeatUtc: reader.GetFieldValue<DateTimeOffset>(3),
                ActiveLeaseCount: reader.GetInt32(4),
                Message: reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedUtc: reader.GetFieldValue<DateTimeOffset>(6)));
        }

        var now = DateTimeOffset.UtcNow;
        var staleWorkers = workers.Count(x => now - x.LastHeartbeatUtc > TimeSpan.FromSeconds(safeStaleAfterSeconds));
        var activeWorkers = workers.Count(x => x.ActiveLeaseCount > 0 && now - x.LastHeartbeatUtc <= TimeSpan.FromSeconds(safeStaleAfterSeconds));
        var idleWorkers = workers.Count - activeWorkers - staleWorkers;

        return new ExecutionWorkerTelemetrySummary(
            TotalWorkers: workers.Count,
            ActiveWorkers: activeWorkers,
            IdleWorkers: Math.Max(0, idleWorkers),
            StaleWorkers: staleWorkers,
            GeneratedUtc: now,
            Workers: workers);
    }

    private string GetConnectionString()
    {
        var connectionString =
            _configuration.GetConnectionString("OperationalSql") ??
            _configuration["OperationalSql:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Operational SQL connection string is not configured.");
        }

        return connectionString;
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}


