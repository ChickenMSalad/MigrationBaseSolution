using Microsoft.Data.SqlClient;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionReplayApprovalService : IExecutionReplayApprovalService
{
    private readonly IConfiguration _configuration;
    private readonly IExecutionReplayPreparationService _preparationService;
    private readonly IOperationalEventStore _eventStore;

    public SqlExecutionReplayApprovalService(
        IConfiguration configuration,
        IExecutionReplayPreparationService preparationService,
        IOperationalEventStore eventStore)
    {
        _configuration = configuration;
        _preparationService = preparationService;
        _eventStore = eventStore;
    }

    public async Task<ExecutionReplayApprovalResult> ApproveAsync(
        ApproveExecutionReplayRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ApprovedBy))
        {
            throw new InvalidOperationException("ApprovedBy is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ApprovalNote))
        {
            throw new InvalidOperationException("ApprovalNote is required.");
        }

        var scope = NormalizeScope(request.Scope);
        var expiresInMinutes = Math.Clamp(request.ExpiresInMinutes, 5, 1440);
        var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes);

        var preparation = await _preparationService.PrepareAsync(
            new PrepareExecutionReplayRequest(request.SourceExecutionSessionId, scope, request.ApprovalNote),
            cancellationToken);

        if (!preparation.CanPrepareReplay)
        {
            throw new InvalidOperationException("Replay approval cannot be granted because preparation did not pass safety checks.");
        }

        await AssertNoActiveReplayConflictAsync(request.SourceExecutionSessionId, cancellationToken);

        var approvalId = Guid.NewGuid();
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO dbo.MigrationExecutionReplayApprovals
(
    ReplayApprovalId,
    SourceExecutionSessionId,
    Scope,
    ApprovedBy,
    ApprovalNote,
    Status,
    ExpiresUtc,
    CreatedUtc
)
VALUES
(
    @ReplayApprovalId,
    @SourceExecutionSessionId,
    @Scope,
    @ApprovedBy,
    @ApprovalNote,
    'approved',
    @ExpiresUtc,
    SYSUTCDATETIME()
);
";

        command.Parameters.AddWithValue("@ReplayApprovalId", approvalId);
        command.Parameters.AddWithValue("@SourceExecutionSessionId", request.SourceExecutionSessionId);
        command.Parameters.AddWithValue("@Scope", scope);
        command.Parameters.AddWithValue("@ApprovedBy", request.ApprovedBy.Trim());
        command.Parameters.AddWithValue("@ApprovalNote", request.ApprovalNote.Trim());
        command.Parameters.AddWithValue("@ExpiresUtc", expiresUtc);

        await command.ExecuteNonQueryAsync(cancellationToken);

        await _eventStore.WriteAsync(
            "ExecutionReplayApproved",
            "info",
            "execution",
            "Migration.Admin.Api",
            $"Replay approved for scope '{scope}' by {request.ApprovedBy.Trim()}.",
            null,
            request.SourceExecutionSessionId,
            null,
            cancellationToken);

        var approval = new ExecutionReplayApprovalRecord(
            approvalId,
            request.SourceExecutionSessionId,
            scope,
            request.ApprovedBy.Trim(),
            request.ApprovalNote.Trim(),
            "approved",
            expiresUtc,
            DateTimeOffset.UtcNow,
            null,
            null);

        return new ExecutionReplayApprovalResult(approval, preparation.Findings);
    }

    public async Task<ExecutionReplayApprovalRecord?> FindActiveApprovalAsync(
        Guid sourceExecutionSessionId,
        string scope,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (1)
    ReplayApprovalId,
    SourceExecutionSessionId,
    Scope,
    ApprovedBy,
    ApprovalNote,
    Status,
    ExpiresUtc,
    CreatedUtc,
    ConsumedUtc,
    ReplayExecutionSessionId
FROM dbo.MigrationExecutionReplayApprovals
WHERE SourceExecutionSessionId = @SourceExecutionSessionId
  AND Scope = @Scope
  AND Status = 'approved'
  AND ExpiresUtc > SYSUTCDATETIME()
ORDER BY CreatedUtc DESC;
";

        command.Parameters.AddWithValue("@SourceExecutionSessionId", sourceExecutionSessionId);
        command.Parameters.AddWithValue("@Scope", NormalizeScope(scope));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadApproval(reader);
    }

    public async Task ConsumeAsync(
        Guid replayApprovalId,
        Guid replayExecutionSessionId,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.MigrationExecutionReplayApprovals
SET
    Status = 'consumed',
    ConsumedUtc = SYSUTCDATETIME(),
    ReplayExecutionSessionId = @ReplayExecutionSessionId
WHERE ReplayApprovalId = @ReplayApprovalId
  AND Status = 'approved'
  AND ExpiresUtc > SYSUTCDATETIME();
";

        command.Parameters.AddWithValue("@ReplayApprovalId", replayApprovalId);
        command.Parameters.AddWithValue("@ReplayExecutionSessionId", replayExecutionSessionId);

        var updated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updated != 1)
        {
            throw new InvalidOperationException("Replay approval could not be consumed because it was missing, expired, or already consumed.");
        }
    }

    private async Task AssertNoActiveReplayConflictAsync(
        Guid sourceExecutionSessionId,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

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

    private static ExecutionReplayApprovalRecord ReadApproval(SqlDataReader reader)
    {
        return new ExecutionReplayApprovalRecord(
            ReplayApprovalId: reader.GetGuid(0),
            SourceExecutionSessionId: reader.GetGuid(1),
            Scope: reader.GetString(2),
            ApprovedBy: reader.GetString(3),
            ApprovalNote: reader.GetString(4),
            Status: reader.GetString(5),
            ExpiresUtc: reader.GetFieldValue<DateTimeOffset>(6),
            CreatedUtc: reader.GetFieldValue<DateTimeOffset>(7),
            ConsumedUtc: reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            ReplayExecutionSessionId: reader.IsDBNull(9) ? null : reader.GetGuid(9));
    }

    private static string NormalizeScope(string? scope)
    {
        var normalized = string.IsNullOrWhiteSpace(scope)
            ? "failed-only"
            : scope.Trim().ToLowerInvariant();

        return normalized switch
        {
            "failed-only" => normalized,
            "dead-letter-only" => normalized,
            "incomplete-only" => normalized,
            "all" => normalized,
            _ => "failed-only"
        };
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
