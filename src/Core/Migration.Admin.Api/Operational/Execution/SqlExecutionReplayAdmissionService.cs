using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionReplayAdmissionService : IExecutionReplayAdmissionService
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<ExecutionReplayAdmissionOptions> _options;
    private readonly IOperationalEventStore _eventStore;

    public SqlExecutionReplayAdmissionService(
        IConfiguration configuration,
        IOptions<ExecutionReplayAdmissionOptions> options,
        IOperationalEventStore eventStore)
    {
        _configuration = configuration;
        _options = options;
        _eventStore = eventStore;
    }

    public async Task<ExecutionReplayAdmissionEvaluationResult> EvaluateAsync(
        EvaluateExecutionReplayAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var take = Math.Clamp(request.Take ?? options.Take, 1, 250);
        var maxConcurrent = Math.Clamp(options.MaxConcurrentReplays, 0, 100);
        var withinWindow = IsWithinAllowedWindow(options, DateTimeOffset.UtcNow);

        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var activeReplayCount = await CountActiveReplaysAsync(connection, cancellationToken);
        var pending = await ReadPendingReplaySessionsAsync(connection, take, cancellationToken);
        var decisions = new List<ExecutionReplayAdmissionDecision>();

        foreach (var session in pending)
        {
            if (!options.Enabled)
            {
                decisions.Add(new ExecutionReplayAdmissionDecision(
                    session.ExecutionSessionId,
                    session.Name,
                    "deferred",
                    "Replay admission is disabled.",
                    session.CreatedUtc));
                continue;
            }

            if (!withinWindow)
            {
                decisions.Add(new ExecutionReplayAdmissionDecision(
                    session.ExecutionSessionId,
                    session.Name,
                    "deferred",
                    "Current UTC time is outside the configured replay admission window.",
                    session.CreatedUtc));
                continue;
            }

            if (activeReplayCount >= maxConcurrent)
            {
                decisions.Add(new ExecutionReplayAdmissionDecision(
                    session.ExecutionSessionId,
                    session.Name,
                    "deferred",
                    $"Active replay concurrency {activeReplayCount} is at or above limit {maxConcurrent}.",
                    session.CreatedUtc));
                continue;
            }

            await AdmitAsync(connection, session.ExecutionSessionId, cancellationToken);
            activeReplayCount++;

            decisions.Add(new ExecutionReplayAdmissionDecision(
                session.ExecutionSessionId,
                session.Name,
                "admitted",
                "Replay session admitted to queued state.",
                session.CreatedUtc));

            await _eventStore.WriteAsync(
                "ExecutionReplayAdmitted",
                "info",
                "execution",
                "Migration.Admin.Api",
                "Replay execution session admitted to queued state.",
                null,
                session.ExecutionSessionId,
                session.MigrationRunId,
                cancellationToken);
        }

        foreach (var deferred in decisions.Where(x => x.Decision == "deferred"))
        {
            await _eventStore.WriteAsync(
                "ExecutionReplayDeferred",
                "info",
                "execution",
                "Migration.Admin.Api",
                deferred.Reason,
                null,
                deferred.ExecutionSessionId,
                null,
                cancellationToken);
        }

        return new ExecutionReplayAdmissionEvaluationResult(
            GeneratedUtc: DateTimeOffset.UtcNow,
            ActiveReplayCount: activeReplayCount,
            MaxConcurrentReplays: maxConcurrent,
            WithinAllowedWindow: withinWindow,
            Decisions: decisions);
    }

    private static bool IsWithinAllowedWindow(
        ExecutionReplayAdmissionOptions options,
        DateTimeOffset now)
    {
        var start = Math.Clamp(options.AllowedStartHourUtc, 0, 23);
        var end = Math.Clamp(options.AllowedEndHourUtc, 1, 24);
        var hour = now.UtcDateTime.Hour;

        if (start < end)
        {
            return hour >= start && hour < end;
        }

        return hour >= start || hour < end;
    }

    private static async Task<int> CountActiveReplaysAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(1)
FROM dbo.MigrationExecutionSessions
WHERE ReplaySourceExecutionSessionId IS NOT NULL
  AND Status IN ('queued', 'running', 'paused');
";

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<IReadOnlyList<PendingReplaySession>> ReadPendingReplaySessionsAsync(
        SqlConnection connection,
        int take,
        CancellationToken cancellationToken)
    {
        var sessions = new List<PendingReplaySession>();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@Take)
    ExecutionSessionId,
    MigrationRunId,
    Name,
    CreatedUtc
FROM dbo.MigrationExecutionSessions
WHERE ReplaySourceExecutionSessionId IS NOT NULL
  AND Status = 'admission-pending'
ORDER BY CreatedUtc ASC;
";

        command.Parameters.AddWithValue("@Take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            sessions.Add(new PendingReplaySession(
                ExecutionSessionId: reader.GetGuid(0),
                MigrationRunId: reader.IsDBNull(1) ? null : reader.GetGuid(1),
                Name: reader.GetString(2),
                CreatedUtc: reader.GetFieldValue<DateTimeOffset>(3)));
        }

        return sessions;
    }

    private static async Task AdmitAsync(
        SqlConnection connection,
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.MigrationExecutionSessions
SET Status = 'queued'
WHERE ExecutionSessionId = @ExecutionSessionId
  AND Status = 'admission-pending';
";

        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);

        var updated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updated != 1)
        {
            throw new InvalidOperationException($"Replay session could not be admitted: {executionSessionId}");
        }
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

    private sealed record PendingReplaySession(
        Guid ExecutionSessionId,
        Guid? MigrationRunId,
        string Name,
        DateTimeOffset CreatedUtc);
}
