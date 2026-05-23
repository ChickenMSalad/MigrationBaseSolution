using Microsoft.Data.SqlClient;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionWorkItemQueueStore : IExecutionWorkItemQueueStore
{
    private readonly IConfiguration _configuration;
    private readonly IOperationalEventStore _eventStore;

    public SqlExecutionWorkItemQueueStore(
        IConfiguration configuration,
        IOperationalEventStore eventStore)
    {
        _configuration = configuration;
        _eventStore = eventStore;
    }

    public async Task<IReadOnlyList<ExecutionWorkItemRecord>> ExpandFromPlanAsync(
        ExpandExecutionPlanToWorkItemsRequest request,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var existsCommand = connection.CreateCommand())
        {
            existsCommand.CommandText = @"
SELECT COUNT(1)
FROM dbo.MigrationExecutionWorkItems
WHERE ExecutionSessionId = @ExecutionSessionId;
";
            existsCommand.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);
            var count = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken));

            if (count > 0)
            {
                return await ReadRecentAsync(request.ExecutionSessionId, 250, cancellationToken);
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
INSERT INTO dbo.MigrationExecutionWorkItems
(
    ExecutionWorkItemId,
    ExecutionSessionId,
    MigrationRunId,
    ExecutionPlanStepId,
    WorkItemType,
    WorkItemName,
    Status,
    Priority,
    CreatedUtc
)
SELECT
    NEWID(),
    ExecutionSessionId,
    MigrationRunId,
    ExecutionPlanStepId,
    StepType,
    StepName,
    'pending',
    StepOrder,
    SYSUTCDATETIME()
FROM dbo.MigrationExecutionPlanSteps
WHERE ExecutionSessionId = @ExecutionSessionId
ORDER BY StepOrder ASC;
";
            command.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await _eventStore.WriteAsync("ExecutionWorkItemsExpanded", "info", "execution", "Migration.Admin.Api",
            "Execution work items expanded from execution plan.", null, request.ExecutionSessionId, null, cancellationToken);

        return await ReadRecentAsync(request.ExecutionSessionId, 250, cancellationToken);
    }

    public async Task<IReadOnlyList<ExecutionWorkItemRecord>> LeaseAsync(
        LeaseExecutionWorkItemsRequest request,
        CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(request.Take, 1, 100);
        var safeLeaseSeconds = Math.Clamp(request.LeaseSeconds, 30, 3600);
        var leaseId = Guid.NewGuid();
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var sessionStatus = await ReadSessionStatusAsync(connection, request.ExecutionSessionId, cancellationToken);
        if (string.Equals(sessionStatus, "paused", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sessionStatus, "cancelled", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sessionStatus, "completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sessionStatus, "failed", StringComparison.OrdinalIgnoreCase))
        {
            await _eventStore.WriteAsync("ExecutionWorkItemLeaseSkipped", "info", "execution", "Migration.Admin.Api",
                $"No work items leased because execution session status is '{sessionStatus}'.", null, request.ExecutionSessionId, null, cancellationToken);

            return Array.Empty<ExecutionWorkItemRecord>();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = @"
DECLARE @leased TABLE (ExecutionWorkItemId UNIQUEIDENTIFIER);

UPDATE TOP (@Take) dbo.MigrationExecutionWorkItems
SET
    Status = 'leased',
    WorkerId = @WorkerId,
    LeaseId = @LeaseId,
    LeaseExpiresUtc = DATEADD(SECOND, @LeaseSeconds, SYSUTCDATETIME()),
    StartedUtc = COALESCE(StartedUtc, SYSUTCDATETIME())
OUTPUT inserted.ExecutionWorkItemId INTO @leased
WHERE ExecutionSessionId = @ExecutionSessionId
  AND
  (
      Status = 'pending'
      OR
      (
          Status = 'leased'
          AND LeaseExpiresUtc < SYSUTCDATETIME()
      )
  );

SELECT
    ExecutionWorkItemId,
    ExecutionSessionId,
    MigrationRunId,
    ExecutionPlanStepId,
    WorkItemType,
    WorkItemName,
    Status,
    Priority,
    RetryCount,
    MaxRetries,
    WorkerId,
    LeaseId,
    LeaseExpiresUtc,
    PayloadJson,
    CreatedUtc,
    StartedUtc,
    CompletedUtc,
    ErrorMessage
FROM dbo.MigrationExecutionWorkItems
WHERE ExecutionWorkItemId IN (SELECT ExecutionWorkItemId FROM @leased)
ORDER BY Priority ASC, CreatedUtc ASC;
";
        command.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);
        command.Parameters.AddWithValue("@WorkerId", request.WorkerId);
        command.Parameters.AddWithValue("@LeaseId", leaseId);
        command.Parameters.AddWithValue("@Take", safeTake);
        command.Parameters.AddWithValue("@LeaseSeconds", safeLeaseSeconds);

        var leased = await ReadItemsAsync(command, cancellationToken);

        if (leased.Count > 0)
        {
            await _eventStore.WriteAsync("ExecutionWorkItemsLeased", "info", "execution", "Migration.Admin.Api",
                $"{leased.Count} execution work item(s) leased by {request.WorkerId}.", null, request.ExecutionSessionId, null, cancellationToken);
        }

        return leased;
    }

    public async Task RenewLeaseAsync(RenewExecutionWorkItemLeaseRequest request, CancellationToken cancellationToken)
    {
        var safeLeaseSeconds = Math.Clamp(request.LeaseSeconds, 30, 3600);
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var executionSessionId = await ReadExecutionSessionIdAsync(connection, request.ExecutionWorkItemId, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.MigrationExecutionWorkItems
SET LeaseExpiresUtc = DATEADD(SECOND, @LeaseSeconds, SYSUTCDATETIME())
WHERE ExecutionWorkItemId = @ExecutionWorkItemId
  AND LeaseId = @LeaseId
  AND WorkerId = @WorkerId
  AND Status = 'leased';
";
        command.Parameters.AddWithValue("@ExecutionWorkItemId", request.ExecutionWorkItemId);
        command.Parameters.AddWithValue("@LeaseId", request.LeaseId);
        command.Parameters.AddWithValue("@WorkerId", request.WorkerId);
        command.Parameters.AddWithValue("@LeaseSeconds", safeLeaseSeconds);

        var updated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updated != 1)
        {
            throw new InvalidOperationException("Work item lease could not be renewed because the lease did not match.");
        }

        await _eventStore.WriteAsync("ExecutionWorkItemLeaseRenewed", "info", "execution", "Migration.Admin.Api",
            $"Execution work item lease renewed by {request.WorkerId}.", null, executionSessionId, null, cancellationToken);
    }

    public async Task<int> RequeueAsync(RequeueExecutionWorkItemsRequest request, CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.MigrationExecutionWorkItems
SET
    Status = 'pending',
    WorkerId = NULL,
    LeaseId = NULL,
    LeaseExpiresUtc = NULL,
    CompletedUtc = NULL,
    ErrorMessage = NULL
WHERE ExecutionSessionId = @ExecutionSessionId
  AND
  (
      (@IncludeFailed = 1 AND Status = 'failed' AND RetryCount < MaxRetries)
      OR
      (@IncludeExpiredLeases = 1 AND Status = 'leased' AND LeaseExpiresUtc < SYSUTCDATETIME())
  );
";
        command.Parameters.AddWithValue("@ExecutionSessionId", request.ExecutionSessionId);
        command.Parameters.AddWithValue("@IncludeFailed", request.IncludeFailed ? 1 : 0);
        command.Parameters.AddWithValue("@IncludeExpiredLeases", request.IncludeExpiredLeases ? 1 : 0);

        var updated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updated > 0)
        {
            await _eventStore.WriteAsync("ExecutionWorkItemsRequeued", "info", "execution", "Migration.Admin.Api",
                $"{updated} execution work item(s) requeued.", null, request.ExecutionSessionId, null, cancellationToken);
        }

        return updated;
    }

    public async Task CompleteAsync(CompleteExecutionWorkItemRequest request, CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var executionSessionId = await ReadExecutionSessionIdAsync(connection, request.ExecutionWorkItemId, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.MigrationExecutionWorkItems
SET Status = 'completed', CompletedUtc = SYSUTCDATETIME(), ErrorMessage = NULL
WHERE ExecutionWorkItemId = @ExecutionWorkItemId AND LeaseId = @LeaseId AND WorkerId = @WorkerId;
";
        command.Parameters.AddWithValue("@ExecutionWorkItemId", request.ExecutionWorkItemId);
        command.Parameters.AddWithValue("@LeaseId", request.LeaseId);
        command.Parameters.AddWithValue("@WorkerId", request.WorkerId);
        var updated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updated != 1) throw new InvalidOperationException("Work item could not be completed because the lease did not match.");

        await _eventStore.WriteAsync("ExecutionWorkItemCompleted", "info", "execution", "Migration.Admin.Api",
            $"Execution work item completed by {request.WorkerId}.", null, executionSessionId, null, cancellationToken);
    }

    public async Task FailAsync(FailExecutionWorkItemRequest request, CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var executionSessionId = await ReadExecutionSessionIdAsync(connection, request.ExecutionWorkItemId, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.MigrationExecutionWorkItems
SET
    RetryCount = RetryCount + 1,
    Status = CASE WHEN RetryCount + 1 >= MaxRetries THEN 'dead-lettered' ELSE 'failed' END,
    ErrorMessage = @ErrorMessage,
    CompletedUtc = SYSUTCDATETIME()
WHERE ExecutionWorkItemId = @ExecutionWorkItemId AND LeaseId = @LeaseId AND WorkerId = @WorkerId;
";
        command.Parameters.AddWithValue("@ExecutionWorkItemId", request.ExecutionWorkItemId);
        command.Parameters.AddWithValue("@LeaseId", request.LeaseId);
        command.Parameters.AddWithValue("@WorkerId", request.WorkerId);
        command.Parameters.AddWithValue("@ErrorMessage", request.ErrorMessage);
        var updated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updated != 1) throw new InvalidOperationException("Work item could not be failed because the lease did not match.");

        await _eventStore.WriteAsync("ExecutionWorkItemFailed", "warning", "execution", "Migration.Admin.Api",
            $"Execution work item failed by {request.WorkerId}: {request.ErrorMessage}", null, executionSessionId, null, cancellationToken);
    }

    public async Task<ExecutionWorkItemQueueSummary> ReadSummaryAsync(Guid executionSessionId, CancellationToken cancellationToken)
    {
        var items = await ReadRecentAsync(executionSessionId, 1000, cancellationToken);
        return new ExecutionWorkItemQueueSummary(
            executionSessionId,
            items.Count,
            items.Count(x => x.Status == "pending"),
            items.Count(x => x.Status == "leased"),
            items.Count(x => x.Status == "running"),
            items.Count(x => x.Status == "completed"),
            items.Count(x => x.Status == "failed"),
            items.Count(x => x.Status == "dead-lettered"));
    }

    public async Task<IReadOnlyList<ExecutionWorkItemRecord>> ReadRecentAsync(Guid executionSessionId, int take, CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 1000);
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@Take)
    ExecutionWorkItemId, ExecutionSessionId, MigrationRunId, ExecutionPlanStepId,
    WorkItemType, WorkItemName, Status, Priority, RetryCount, MaxRetries,
    WorkerId, LeaseId, LeaseExpiresUtc, PayloadJson, CreatedUtc, StartedUtc, CompletedUtc, ErrorMessage
FROM dbo.MigrationExecutionWorkItems
WHERE ExecutionSessionId = @ExecutionSessionId
ORDER BY Priority ASC, CreatedUtc ASC;
";
        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);
        command.Parameters.AddWithValue("@Take", safeTake);

        return await ReadItemsAsync(command, cancellationToken);
    }

    private static async Task<string> ReadSessionStatusAsync(SqlConnection connection, Guid executionSessionId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Status FROM dbo.MigrationExecutionSessions WHERE ExecutionSessionId = @ExecutionSessionId;";
        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null) throw new InvalidOperationException($"Execution session was not found: {executionSessionId}");
        return Convert.ToString(result) ?? "unknown";
    }

    private static async Task<Guid?> ReadExecutionSessionIdAsync(SqlConnection connection, Guid executionWorkItemId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT ExecutionSessionId FROM dbo.MigrationExecutionWorkItems WHERE ExecutionWorkItemId = @ExecutionWorkItemId;";
        command.Parameters.AddWithValue("@ExecutionWorkItemId", executionWorkItemId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid value ? value : null;
    }

    private static async Task<IReadOnlyList<ExecutionWorkItemRecord>> ReadItemsAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        var items = new List<ExecutionWorkItemRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ExecutionWorkItemRecord(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.IsDBNull(2) ? null : reader.GetGuid(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.GetInt32(8),
                reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetGuid(11),
                reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                reader.GetFieldValue<DateTimeOffset>(14),
                reader.IsDBNull(15) ? null : reader.GetFieldValue<DateTimeOffset>(15),
                reader.IsDBNull(16) ? null : reader.GetFieldValue<DateTimeOffset>(16),
                reader.IsDBNull(17) ? null : reader.GetString(17)));
        }
        return items;
    }

    private string GetConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("OperationalSql") ?? _configuration["OperationalSql:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Operational SQL connection string is not configured.");
        return connectionString;
    }
}
