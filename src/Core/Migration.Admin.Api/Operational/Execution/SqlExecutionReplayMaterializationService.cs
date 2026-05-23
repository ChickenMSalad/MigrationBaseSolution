using Microsoft.Data.SqlClient;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionReplayMaterializationService : IExecutionReplayMaterializationService
{
    private const int MaxReplayDepth = 3;

    private readonly IConfiguration _configuration;
    private readonly IExecutionReplayPreparationService _preparationService;
    private readonly IExecutionReplayApprovalService _approvalService;
    private readonly IExecutionReplayPolicyService _policyService;
    private readonly IOperationalEventStore _eventStore;

    public SqlExecutionReplayMaterializationService(
        IConfiguration configuration,
        IExecutionReplayPreparationService preparationService,
        IExecutionReplayApprovalService approvalService,
        IExecutionReplayPolicyService policyService,
        IOperationalEventStore eventStore)
    {
        _configuration = configuration;
        _preparationService = preparationService;
        _approvalService = approvalService;
        _policyService = policyService;
        _eventStore = eventStore;
    }

    public async Task<ExecutionReplayMaterializationResult> MaterializeAsync(
        MaterializeExecutionReplayRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ApprovalNote))
        {
            throw new InvalidOperationException("Replay approval note is required.");
        }

        var policy = await _policyService.EvaluateAsync(
            request.SourceExecutionSessionId,
            request.Scope,
            cancellationToken);

        if (policy.Decision == "block")
        {
            throw new InvalidOperationException("Replay materialization was blocked by replay policy.");
        }

        var approval = await _approvalService.FindActiveApprovalAsync(
            request.SourceExecutionSessionId,
            request.Scope,
            cancellationToken);

        if (approval is null)
        {
            throw new InvalidOperationException("An active replay approval is required before materialization.");
        }

        var preparation = await _preparationService.PrepareAsync(
            new PrepareExecutionReplayRequest(request.SourceExecutionSessionId, request.Scope, request.ApprovalNote),
            cancellationToken);

        if (!preparation.CanPrepareReplay || preparation.Items.Count == 0)
        {
            throw new InvalidOperationException("Replay cannot be materialized from the prepared manifest.");
        }

        var connectionString = GetConnectionString();
        var replaySessionId = Guid.NewGuid();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var source = await ReadSourceSessionAsync(connection, request.SourceExecutionSessionId, cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Source execution session was not found: {request.SourceExecutionSessionId}");
        }

        if (source.ReplayDepth >= MaxReplayDepth)
        {
            throw new InvalidOperationException($"Replay depth limit exceeded. Maximum replay depth is {MaxReplayDepth}.");
        }

        await AssertNoActiveReplayConflictAsync(connection, request.SourceExecutionSessionId, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var sessionCommand = connection.CreateCommand())
        {
            sessionCommand.Transaction = (SqlTransaction)transaction;
            sessionCommand.CommandText = @"
INSERT INTO dbo.MigrationExecutionSessions
(
    ExecutionSessionId,
    MigrationRunId,
    Name,
    SourceConnector,
    TargetConnector,
    Status,
    CreatedUtc,
    Notes,
    ReplaySourceExecutionSessionId,
    ReplayScope,
    ReplayDepth,
    ReplayApprovalNote
)
VALUES
(
    @ExecutionSessionId,
    @MigrationRunId,
    @Name,
    @SourceConnector,
    @TargetConnector,
    'queued',
    SYSUTCDATETIME(),
    @Notes,
    @ReplaySourceExecutionSessionId,
    @ReplayScope,
    @ReplayDepth,
    @ReplayApprovalNote
);
";
            sessionCommand.Parameters.AddWithValue("@ExecutionSessionId", replaySessionId);
            sessionCommand.Parameters.AddWithValue("@MigrationRunId", (object?)source.MigrationRunId ?? DBNull.Value);
            sessionCommand.Parameters.AddWithValue("@Name", $"Replay of {source.Name}");
            sessionCommand.Parameters.AddWithValue("@SourceConnector", DbValue(source.SourceConnector));
            sessionCommand.Parameters.AddWithValue("@TargetConnector", DbValue(source.TargetConnector));
            sessionCommand.Parameters.AddWithValue("@Notes", $"Replay session derived from {request.SourceExecutionSessionId:D}. Policy decision: {policy.Decision}.");
            sessionCommand.Parameters.AddWithValue("@ReplaySourceExecutionSessionId", request.SourceExecutionSessionId);
            sessionCommand.Parameters.AddWithValue("@ReplayScope", preparation.Scope);
            sessionCommand.Parameters.AddWithValue("@ReplayDepth", source.ReplayDepth + 1);
            sessionCommand.Parameters.AddWithValue("@ReplayApprovalNote", approval.ApprovalNote);

            await sessionCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var item in preparation.Items.OrderBy(x => x.ReplayOrder))
        {
            await using var workCommand = connection.CreateCommand();
            workCommand.Transaction = (SqlTransaction)transaction;
            workCommand.CommandText = @"
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
    PayloadJson,
    CreatedUtc
)
VALUES
(
    NEWID(),
    @ExecutionSessionId,
    @MigrationRunId,
    @ExecutionPlanStepId,
    @WorkItemType,
    @WorkItemName,
    'pending',
    @Priority,
    @PayloadJson,
    SYSUTCDATETIME()
);
";
            workCommand.Parameters.AddWithValue("@ExecutionSessionId", replaySessionId);
            workCommand.Parameters.AddWithValue("@MigrationRunId", (object?)source.MigrationRunId ?? DBNull.Value);
            workCommand.Parameters.AddWithValue("@ExecutionPlanStepId", (object?)item.SourceExecutionPlanStepId ?? DBNull.Value);
            workCommand.Parameters.AddWithValue("@WorkItemType", item.ReplayType);
            workCommand.Parameters.AddWithValue("@WorkItemName", $"Replay: {item.ReplayName}");
            workCommand.Parameters.AddWithValue("@Priority", item.ReplayOrder);
            workCommand.Parameters.AddWithValue("@PayloadJson", DbValue(item.PayloadJson));

            await workCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        await _approvalService.ConsumeAsync(approval.ReplayApprovalId, replaySessionId, cancellationToken);

        await _eventStore.WriteAsync(
            "ExecutionReplayMaterialized",
            "info",
            "execution",
            "Migration.Admin.Api",
            $"Replay execution session materialized from {request.SourceExecutionSessionId:D} with {preparation.Items.Count} work item(s). Policy decision: {policy.Decision}.",
            null,
            replaySessionId,
            source.MigrationRunId,
            cancellationToken);

        return new ExecutionReplayMaterializationResult(
            request.SourceExecutionSessionId,
            replaySessionId,
            preparation.Scope,
            source.ReplayDepth + 1,
            preparation.Items.Count,
            DateTimeOffset.UtcNow,
            preparation.Findings);
    }

    private async Task AssertNoActiveReplayConflictAsync(SqlConnection connection, Guid sourceExecutionSessionId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(1)
FROM dbo.MigrationExecutionSessions
WHERE ReplaySourceExecutionSessionId = @SourceExecutionSessionId
  AND Status IN ('created', 'validating', 'manifest-loading', 'queued', 'running', 'paused');
";
        command.Parameters.AddWithValue("@SourceExecutionSessionId", sourceExecutionSessionId);
        var activeCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (activeCount > 0)
        {
            throw new InvalidOperationException("An active replay already exists for this source execution session.");
        }
    }

    private async Task<SourceSession?> ReadSourceSessionAsync(SqlConnection connection, Guid executionSessionId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT MigrationRunId, Name, SourceConnector, TargetConnector, ReplayDepth
FROM dbo.MigrationExecutionSessions
WHERE ExecutionSessionId = @ExecutionSessionId;
";
        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SourceSession(
            reader.IsDBNull(0) ? null : reader.GetGuid(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetInt32(4));
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

    private static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private sealed record SourceSession(Guid? MigrationRunId, string Name, string? SourceConnector, string? TargetConnector, int ReplayDepth);
}
