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
            ExecutionReplayAdmissionDecision decision;

            if (!options.Enabled)
            {
                decision = new ExecutionReplayAdmissionDecision(session.ExecutionSessionId, session.Name, "deferred", "Replay admission is disabled.", session.CreatedUtc);
            }
            else if (!withinWindow)
            {
                decision = new ExecutionReplayAdmissionDecision(session.ExecutionSessionId, session.Name, "deferred", "Current UTC time is outside the configured replay admission window.", session.CreatedUtc);
            }
            else if (activeReplayCount >= maxConcurrent)
            {
                decision = new ExecutionReplayAdmissionDecision(session.ExecutionSessionId, session.Name, "deferred", $"Active replay concurrency {activeReplayCount} is at or above limit {maxConcurrent}.", session.CreatedUtc);
            }
            else
            {
                await AdmitAsync(connection, session.ExecutionSessionId, cancellationToken);
                activeReplayCount++;

                decision = new ExecutionReplayAdmissionDecision(session.ExecutionSessionId, session.Name, "admitted", "Replay session admitted to queued state.", session.CreatedUtc);

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

            decisions.Add(decision);

            await PersistDecisionAsync(
                connection,
                decision,
                activeReplayCount,
                maxConcurrent,
                withinWindow,
                cancellationToken);

            if (decision.Decision == "deferred")
            {
                await _eventStore.WriteAsync(
                    "ExecutionReplayDeferred",
                    "info",
                    "execution",
                    "Migration.Admin.Api",
                    decision.Reason,
                    null,
                    decision.ExecutionSessionId,
                    session.MigrationRunId,
                    cancellationToken);
            }
        }

        return new ExecutionReplayAdmissionEvaluationResult(
            GeneratedUtc: DateTimeOffset.UtcNow,
            ActiveReplayCount: activeReplayCount,
            MaxConcurrentReplays: maxConcurrent,
            WithinAllowedWindow: withinWindow,
            Decisions: decisions);
    }

    public async Task<IReadOnlyList<ExecutionReplayAdmissionDecisionRecord>> ReadHistoryAsync(
        Guid executionSessionId,
        int take,
        CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 250);
        var decisions = new List<ExecutionReplayAdmissionDecisionRecord>();
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@Take)
    ReplayAdmissionDecisionId,
    ExecutionSessionId,
    Decision,
    Reason,
    ActiveReplayCount,
    MaxConcurrentReplays,
    WithinAllowedWindow,
    CreatedUtc
FROM dbo.MigrationExecutionReplayAdmissionDecisions
WHERE ExecutionSessionId = @ExecutionSessionId
ORDER BY CreatedUtc DESC;
";
        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);
        command.Parameters.AddWithValue("@Take", safeTake);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            decisions.Add(new ExecutionReplayAdmissionDecisionRecord(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetBoolean(6),
                reader.GetFieldValue<DateTimeOffset>(7)));
        }

        return decisions;
    }

    private static async Task PersistDecisionAsync(
        SqlConnection connection,
        ExecutionReplayAdmissionDecision decision,
        int activeReplayCount,
        int maxConcurrentReplays,
        bool withinAllowedWindow,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO dbo.MigrationExecutionReplayAdmissionDecisions
(
    ReplayAdmissionDecisionId,
    ExecutionSessionId,
    Decision,
    Reason,
    ActiveReplayCount,
    MaxConcurrentReplays,
    WithinAllowedWindow,
    CreatedUtc
)
VALUES
(
    NEWID(),
    @ExecutionSessionId,
    @Decision,
    @Reason,
    @ActiveReplayCount,
    @MaxConcurrentReplays,
    @WithinAllowedWindow,
    SYSUTCDATETIME()
);
";
        command.Parameters.AddWithValue("@ExecutionSessionId", decision.ExecutionSessionId);
        command.Parameters.AddWithValue("@Decision", decision.Decision);
        command.Parameters.AddWithValue("@Reason", decision.Reason);
        command.Parameters.AddWithValue("@ActiveReplayCount", activeReplayCount);
        command.Parameters.AddWithValue("@MaxConcurrentReplays", maxConcurrentReplays);
        command.Parameters.AddWithValue("@WithinAllowedWindow", withinAllowedWindow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsWithinAllowedWindow(ExecutionReplayAdmissionOptions options, DateTimeOffset now)
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

    private static async Task<int> CountActiveReplaysAsync(SqlConnection connection, CancellationToken cancellationToken)
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

    private static async Task<IReadOnlyList<PendingReplaySession>> ReadPendingReplaySessionsAsync(SqlConnection connection, int take, CancellationToken cancellationToken)
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
                reader.GetGuid(0),
                reader.IsDBNull(1) ? null : reader.GetGuid(1),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3)));
        }

        return sessions;
    }

    private static async Task AdmitAsync(SqlConnection connection, Guid executionSessionId, CancellationToken cancellationToken)
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
        var connectionString = _configuration.GetConnectionString("OperationalSql") ?? _configuration["OperationalSql:ConnectionString"];
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


