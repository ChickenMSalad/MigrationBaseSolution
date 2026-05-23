using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionReplayAdmissionManualService : IExecutionReplayAdmissionManualService
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<ExecutionReplayAdmissionOptions> _options;
    private readonly IOperationalEventStore _eventStore;

    public SqlExecutionReplayAdmissionManualService(
        IConfiguration configuration,
        IOptions<ExecutionReplayAdmissionOptions> options,
        IOperationalEventStore eventStore)
    {
        _configuration = configuration;
        _options = options;
        _eventStore = eventStore;
    }

    public Task<ReplayAdmissionManualDecisionResult> ForceAdmitAsync(
        ReplayAdmissionManualDecisionRequest request,
        CancellationToken cancellationToken)
    {
        return ApplyManualDecisionAsync(
            request,
            decision: "force-admitted",
            targetStatus: "queued",
            eventType: "ExecutionReplayForceAdmitted",
            cancellationToken);
    }

    public Task<ReplayAdmissionManualDecisionResult> ForceDeferAsync(
        ReplayAdmissionManualDecisionRequest request,
        CancellationToken cancellationToken)
    {
        return ApplyManualDecisionAsync(
            request,
            decision: "force-deferred",
            targetStatus: "admission-pending",
            eventType: "ExecutionReplayForceDeferred",
            cancellationToken);
    }

    private async Task<ReplayAdmissionManualDecisionResult> ApplyManualDecisionAsync(
        ReplayAdmissionManualDecisionRequest request,
        string decision,
        string targetStatus,
        string eventType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Operator))
        {
            throw new InvalidOperationException("Operator is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }

        var options = _options.Value;
        var reason = $"{request.Reason.Trim()} Operator: {request.Operator.Trim()}";
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var session = await ReadReplaySessionAsync(connection, request.ExecutionSessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException($"Replay execution session was not found: {request.ExecutionSessionId}");
        }

        if (!session.IsReplay)
        {
            throw new InvalidOperationException("Manual replay admission decisions can only be applied to replay sessions.");
        }

        if (session.Status is not ("admission-pending" or "queued"))
        {
            throw new InvalidOperationException($"Replay session status '{session.Status}' cannot be manually admitted or deferred.");
        }

        if (decision == "force-admitted")
        {
            await UpdateSessionStatusAsync(connection, request.ExecutionSessionId, targetStatus, cancellationToken);
        }

        await PersistDecisionAsync(
            connection,
            request.ExecutionSessionId,
            decision,
            reason,
            activeReplayCount: await CountActiveReplaysAsync(connection, cancellationToken),
            maxConcurrentReplays: Math.Clamp(options.MaxConcurrentReplays, 0, 100),
            withinAllowedWindow: IsWithinAllowedWindow(options, DateTimeOffset.UtcNow),
            cancellationToken);

        await _eventStore.WriteAsync(
            eventType,
            decision == "force-admitted" ? "warning" : "info",
            "execution",
            "Migration.Admin.Api",
            reason,
            null,
            request.ExecutionSessionId,
            session.MigrationRunId,
            cancellationToken);

        return new ReplayAdmissionManualDecisionResult(
            request.ExecutionSessionId,
            decision,
            reason,
            DateTimeOffset.UtcNow);
    }

    private static async Task<ReplaySession?> ReadReplaySessionAsync(
        SqlConnection connection,
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    MigrationRunId,
    Status,
    ReplaySourceExecutionSessionId
FROM dbo.MigrationExecutionSessions
WHERE ExecutionSessionId = @ExecutionSessionId;
";
        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ReplaySession(
            MigrationRunId: reader.IsDBNull(0) ? null : reader.GetGuid(0),
            Status: reader.GetString(1),
            IsReplay: !reader.IsDBNull(2));
    }

    private static async Task UpdateSessionStatusAsync(
        SqlConnection connection,
        Guid executionSessionId,
        string status,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.MigrationExecutionSessions
SET Status = @Status
WHERE ExecutionSessionId = @ExecutionSessionId
  AND ReplaySourceExecutionSessionId IS NOT NULL;
";
        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);
        command.Parameters.AddWithValue("@Status", status);

        var updated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updated != 1)
        {
            throw new InvalidOperationException($"Replay session status could not be updated: {executionSessionId}");
        }
    }

    private static async Task PersistDecisionAsync(
        SqlConnection connection,
        Guid executionSessionId,
        string decision,
        string reason,
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
        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);
        command.Parameters.AddWithValue("@Decision", decision);
        command.Parameters.AddWithValue("@Reason", reason);
        command.Parameters.AddWithValue("@ActiveReplayCount", activeReplayCount);
        command.Parameters.AddWithValue("@MaxConcurrentReplays", maxConcurrentReplays);
        command.Parameters.AddWithValue("@WithinAllowedWindow", withinAllowedWindow);

        await command.ExecuteNonQueryAsync(cancellationToken);
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

    private sealed record ReplaySession(
        Guid? MigrationRunId,
        string Status,
        bool IsReplay);
}
