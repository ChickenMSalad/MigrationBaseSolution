using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionReplayAdmissionHealthService : IExecutionReplayAdmissionHealthService
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<ExecutionReplayAdmissionHealthOptions> _options;
    private readonly IOperationalEventStore _eventStore;

    public SqlExecutionReplayAdmissionHealthService(
        IConfiguration configuration,
        IOptions<ExecutionReplayAdmissionHealthOptions> options,
        IOperationalEventStore eventStore)
    {
        _configuration = configuration;
        _options = options;
        _eventStore = eventStore;
    }

    public async Task<ExecutionReplayAdmissionHealthResult> EvaluateAsync(
        EvaluateExecutionReplayAdmissionHealthRequest request,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var staleMinutes = Math.Clamp(options.StalePendingMinutes, 1, 10080);
        var take = Math.Clamp(request.Take ?? options.Take, 1, 250);
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var pendingCount = await CountPendingAsync(connection, cancellationToken);
        var staleSessions = await ReadStalePendingAsync(connection, staleMinutes, take, cancellationToken);

        if (request.EmitEvents)
        {
            foreach (var session in staleSessions)
            {
                await _eventStore.WriteAsync(
                    "ExecutionReplayAdmissionStalePending",
                    "warning",
                    "execution",
                    "Migration.Admin.Api",
                    $"Replay session has been admission-pending for {session.AgeMinutes} minute(s).",
                    null,
                    session.ExecutionSessionId,
                    null,
                    cancellationToken);
            }
        }

        return new ExecutionReplayAdmissionHealthResult(
            GeneratedUtc: DateTimeOffset.UtcNow,
            StalePendingMinutes: staleMinutes,
            PendingCount: pendingCount,
            StalePendingCount: staleSessions.Count,
            StalePendingSessions: staleSessions);
    }

    private static async Task<int> CountPendingAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(1)
FROM dbo.MigrationExecutionSessions
WHERE ReplaySourceExecutionSessionId IS NOT NULL
  AND Status = 'admission-pending';
";

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<IReadOnlyList<ExecutionReplayAdmissionStalePendingSession>> ReadStalePendingAsync(
        SqlConnection connection,
        int staleMinutes,
        int take,
        CancellationToken cancellationToken)
    {
        var sessions = new List<ExecutionReplayAdmissionStalePendingSession>();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@Take)
    ExecutionSessionId,
    Name,
    Status,
    ReplayScope,
    ReplayDepth,
    CreatedUtc,
    DATEDIFF(MINUTE, CreatedUtc, SYSUTCDATETIME()) AS AgeMinutes
FROM dbo.MigrationExecutionSessions
WHERE ReplaySourceExecutionSessionId IS NOT NULL
  AND Status = 'admission-pending'
  AND CreatedUtc <= DATEADD(MINUTE, -@StaleMinutes, SYSUTCDATETIME())
ORDER BY CreatedUtc ASC;
";

        command.Parameters.AddWithValue("@Take", take);
        command.Parameters.AddWithValue("@StaleMinutes", staleMinutes);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            sessions.Add(new ExecutionReplayAdmissionStalePendingSession(
                ExecutionSessionId: reader.GetGuid(0),
                Name: reader.GetString(1),
                Status: reader.GetString(2),
                ReplayScope: reader.IsDBNull(3) ? null : reader.GetString(3),
                ReplayDepth: reader.GetInt32(4),
                CreatedUtc: reader.GetFieldValue<DateTimeOffset>(5),
                AgeMinutes: reader.GetInt32(6)));
        }

        return sessions;
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
}
