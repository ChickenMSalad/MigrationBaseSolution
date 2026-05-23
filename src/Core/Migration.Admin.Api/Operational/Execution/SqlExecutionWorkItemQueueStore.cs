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

        await _eventStore.WriteAsync(
            eventType: "ExecutionWorkItemsExpanded",
            severity: "info",
            category: "execution",
            source: "Migration.Admin.Api",
            message: "Execution work items expanded from execution plan.",
            payloadJson: null,
            executionSessionId: request.ExecutionSessionId,
            migrationRunId: null,
            cancellationToken: cancellationToken);

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
            await _eventStore.WriteAsync(
                eventType: "ExecutionWorkItemsLeased",
                severity: "info",
                category: "execution",
                source: "Migration.Admin.Api",
                message: $"{leased.Count} execution work item(s) leased by {request.WorkerId}.",
                payloadJson: null,
                executionSessionId: request.ExecutionSessionId,
                migrationRunId: null,
                cancellationToken: cancellationToken);
        }

        return leased;
    }

    public async Task RenewLeaseAsync(
        RenewExecutionWorkItemLeaseRequest request,
        CancellationToken cancellationToken)
    {
        var safeLeaseSeconds = Math.Clamp(request.LeaseSeconds, 30, 3600);
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var executionSessionId = await ReadExecutionSessionIdAsync(
            connection,
            request.ExecutionWorkItemId,
            cancellationToken);

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

        await _eventStore.WriteAsync(
            eventType: "ExecutionWorkItemLeaseRenewed",
            severity: "info",
            category: "execution",
            source: "Migration.Admin.Api",
            message: $"Execution work item lease renewed by {request.WorkerId}.",
            payloadJson: null,
            executionSessionId: executionSessionId,
            migrationRunId: null,
            cancellationToken: cancellationToken);
    }

    public async Task<int> RequeueAsync(
        RequeueExecutionWorkItemsRequest request,
        CancellationToken cancellationToken)
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
            await _eventStore.WriteAsync(
                eventType: "ExecutionWorkItemsRequeued",
                severity: "info",
                category: "execution",
                source: "Migration.Admin.Api",
                message: $"{updated} execution work item(s) requeued.",
                payloadJson: null,
                executionSessionId: request.ExecutionSessionId,
                migrationRunId: null,
                cancellationToken: cancellationToken);
        }

        return updated;
    }

    public async Task CompleteAsync(
        CompleteExecutionWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var executionSessionId = await ReadExecutionSessionIdAsync(
            connection,
            request.ExecutionWorkItemId,
            cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.MigrationExecutionWorkItems
SET
    Status = 'completed',
    CompletedUtc = SYSUTCDATETIME(),
    ErrorMessage = NULL
WHERE ExecutionWorkItemId = @ExecutionWorkItemId
  AND LeaseId = @LeaseId
  AND WorkerId = @WorkerId;
";
        command.Parameters.AddWithValue("@ExecutionWorkItemId", request.ExecutionWorkItemId);
        command.Parameters.AddWithValue("@LeaseId", request.LeaseId);
        command.Parameters.AddWithValue("@WorkerId", request.WorkerId);

        var updated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updated != 1)
        {
            throw new InvalidOperationException("Work item could not be completed because the lease did not match.");
        }

        await _eventStore.WriteAsync(
            eventType: "ExecutionWorkItemCompleted",
            severity: "info",
            category: "execution",
            source: "Migration.Admin.Api",
            message: $"Execution work item completed by {request.WorkerId}.",
            payloadJson: null,
            executionSessionId: executionSessionId,
            migrationRunId: null,
            cancellationToken: cancellationToken);
    }

    public async Task FailAsync(
        FailExecutionWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var executionSessionId = await ReadExecutionSessionIdAsync(
            connection,
            request.ExecutionWorkItemId,
            cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.MigrationExecutionWorkItems
SET
    RetryCount = RetryCount + 1,
    Status = CASE
        WHEN RetryCount + 1 >= MaxRetries THEN 'dead-lettered'
        ELSE 'failed'
    END,
    ErrorMessage = @ErrorMessage,
    CompletedUtc = SYSUTCDATETIME()
WHERE ExecutionWorkItemId = @ExecutionWorkItemId
  AND LeaseId = @LeaseId
  AND WorkerId = @WorkerId;
";
        command.Parameters.AddWithValue("@ExecutionWorkItemId", request.ExecutionWorkItemId);
        command.Parameters.AddWithValue("@LeaseId", request.LeaseId);
        command.Parameters.AddWithValue("@WorkerId", request.WorkerId);
        command.Parameters.AddWithValue("@ErrorMessage", request.ErrorMessage);

        var updated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updated != 1)
        {
            throw new InvalidOperationException("Work item could not be failed because the lease did not match.");
        }

        await _eventStore.WriteAsync(
            eventType: "ExecutionWorkItemFailed",
            severity: "warning",
            category: "execution",
            source: "Migration.Admin.Api",
            message: $"Execution work item failed by {request.WorkerId}: {request.ErrorMessage}",
            payloadJson: null,
            executionSessionId: executionSessionId,
            migrationRunId: null,
            cancellationToken: cancellationToken);
    }

    public async Task<ExecutionWorkItemQueueSummary> ReadSummaryAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        var items = await ReadRecentAsync(executionSessionId, 1000, cancellationToken);

        return new ExecutionWorkItemQueueSummary(
            ExecutionSessionId: executionSessionId,
            Total: items.Count,
            Pending: items.Count(x => x.Status == "pending"),
            Leased: items.Count(x => x.Status == "leased"),
            Running: items.Count(x => x.Status == "running"),
            Completed: items.Count(x => x.Status == "completed"),
            Failed: items.Count(x => x.Status == "failed"),
            DeadLettered: items.Count(x => x.Status == "dead-lettered"));
    }

    public async Task<IReadOnlyList<ExecutionWorkItemRecord>> ReadRecentAsync(
        Guid executionSessionId,
        int take,
        CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 1000);
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@Take)
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
WHERE ExecutionSessionId = @ExecutionSessionId
ORDER BY Priority ASC, CreatedUtc ASC;
";
        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);
        command.Parameters.AddWithValue("@Take", safeTake);

        return await ReadItemsAsync(command, cancellationToken);
    }

    private static async Task<Guid?> ReadExecutionSessionIdAsync(
        SqlConnection connection,
        Guid executionWorkItemId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ExecutionSessionId
FROM dbo.MigrationExecutionWorkItems
WHERE ExecutionWorkItemId = @ExecutionWorkItemId;
";
        command.Parameters.AddWithValue("@ExecutionWorkItemId", executionWorkItemId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid value ? value : null;
    }

    private static async Task<IReadOnlyList<ExecutionWorkItemRecord>> ReadItemsAsync(
        SqlCommand command,
        CancellationToken cancellationToken)
    {
        var items = new List<ExecutionWorkItemRecord>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ExecutionWorkItemRecord(
                ExecutionWorkItemId: reader.GetGuid(0),
                ExecutionSessionId: reader.GetGuid(1),
                MigrationRunId: reader.IsDBNull(2) ? null : reader.GetGuid(2),
                ExecutionPlanStepId: reader.IsDBNull(3) ? null : reader.GetGuid(3),
                WorkItemType: reader.GetString(4),
                WorkItemName: reader.GetString(5),
                Status: reader.GetString(6),
                Priority: reader.GetInt32(7),
                RetryCount: reader.GetInt32(8),
                MaxRetries: reader.GetInt32(9),
                WorkerId: reader.IsDBNull(10) ? null : reader.GetString(10),
                LeaseId: reader.IsDBNull(11) ? null : reader.GetGuid(11),
                LeaseExpiresUtc: reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12),
                PayloadJson: reader.IsDBNull(13) ? null : reader.GetString(13),
                CreatedUtc: reader.GetFieldValue<DateTimeOffset>(14),
                StartedUtc: reader.IsDBNull(15) ? null : reader.GetFieldValue<DateTimeOffset>(15),
                CompletedUtc: reader.IsDBNull(16) ? null : reader.GetFieldValue<DateTimeOffset>(16),
                ErrorMessage: reader.IsDBNull(17) ? null : reader.GetString(17)));
        }

        return items;
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
