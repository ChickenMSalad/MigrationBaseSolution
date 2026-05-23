using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionReplayPolicyService : IExecutionReplayPolicyService
{
    private const int MaxReplayDepth = 3;
    private const int WarnPreparedItemCount = 250;
    private const int BlockPreparedItemCount = 1000;
    private const decimal WarnDeadLetteredPercent = 10m;
    private const decimal BlockDeadLetteredPercent = 25m;

    private readonly IConfiguration _configuration;
    private readonly IExecutionDiagnosticExportService _exportService;
    private readonly IExecutionReplayPreparationService _preparationService;

    public SqlExecutionReplayPolicyService(
        IConfiguration configuration,
        IExecutionDiagnosticExportService exportService,
        IExecutionReplayPreparationService preparationService)
    {
        _configuration = configuration;
        _exportService = exportService;
        _preparationService = preparationService;
    }

    public async Task<ExecutionReplayPolicyEvaluationResult> EvaluateAsync(
        Guid sourceExecutionSessionId,
        string scope,
        CancellationToken cancellationToken)
    {
        var normalizedScope = NormalizeScope(scope);
        var bundle = await _exportService.BuildBundleAsync(sourceExecutionSessionId, cancellationToken);
        var preparation = await _preparationService.PrepareAsync(
            new PrepareExecutionReplayRequest(sourceExecutionSessionId, normalizedScope, "policy evaluation"),
            cancellationToken);

        var replayDepth = await ReadReplayDepthAsync(sourceExecutionSessionId, cancellationToken);
        var totalWorkItems = bundle.WorkItems.Count;
        var failedWorkItems = bundle.WorkItems.Count(x => x.Status == "failed");
        var deadLetteredWorkItems = bundle.WorkItems.Count(x => x.Status == "dead-lettered");
        var preparedItemCount = preparation.Items.Count;
        var activeReplayCount = await CountActiveReplayChildrenAsync(sourceExecutionSessionId, cancellationToken);
        var deadLetteredPercent = totalWorkItems == 0
            ? 0m
            : decimal.Round((deadLetteredWorkItems / (decimal)totalWorkItems) * 100m, 2);

        var violations = new List<ExecutionReplayPolicyViolation>();
        var score = 0;

        if (bundle.Session is null)
        {
            violations.Add(new ExecutionReplayPolicyViolation("block", "source-session-missing", "The source execution session does not exist."));
            score += 100;
        }

        if (replayDepth >= MaxReplayDepth)
        {
            violations.Add(new ExecutionReplayPolicyViolation("block", "replay-depth-limit", $"Replay depth {replayDepth} meets or exceeds the maximum depth of {MaxReplayDepth}."));
            score += 100;
        }
        else if (replayDepth == MaxReplayDepth - 1)
        {
            violations.Add(new ExecutionReplayPolicyViolation("warn", "replay-depth-near-limit", $"Replay depth {replayDepth} is one level below the maximum depth of {MaxReplayDepth}."));
            score += 20;
        }

        if (activeReplayCount > 0)
        {
            violations.Add(new ExecutionReplayPolicyViolation("block", "active-replay-conflict", $"{activeReplayCount} active replay session(s) already exist for this source session."));
            score += 100;
        }

        if (preparedItemCount == 0)
        {
            violations.Add(new ExecutionReplayPolicyViolation("block", "empty-replay-manifest", "The selected scope produced no replay items."));
            score += 80;
        }
        else if (preparedItemCount > BlockPreparedItemCount)
        {
            violations.Add(new ExecutionReplayPolicyViolation("block", "replay-volume-too-large", $"Replay manifest contains {preparedItemCount} items, above the block threshold of {BlockPreparedItemCount}."));
            score += 100;
        }
        else if (preparedItemCount > WarnPreparedItemCount)
        {
            violations.Add(new ExecutionReplayPolicyViolation("warn", "replay-volume-large", $"Replay manifest contains {preparedItemCount} items, above the warning threshold of {WarnPreparedItemCount}."));
            score += 25;
        }

        if (deadLetteredPercent >= BlockDeadLetteredPercent)
        {
            violations.Add(new ExecutionReplayPolicyViolation("block", "dead-letter-pressure-high", $"Dead-lettered work item percentage is {deadLetteredPercent}%, above the block threshold of {BlockDeadLetteredPercent}%."));
            score += 100;
        }
        else if (deadLetteredPercent >= WarnDeadLetteredPercent)
        {
            violations.Add(new ExecutionReplayPolicyViolation("warn", "dead-letter-pressure-elevated", $"Dead-lettered work item percentage is {deadLetteredPercent}%, above the warning threshold of {WarnDeadLetteredPercent}%."));
            score += 30;
        }

        if (normalizedScope == "all")
        {
            violations.Add(new ExecutionReplayPolicyViolation("warn", "all-scope-replay", "Replay scope 'all' can duplicate completed work and requires extra idempotency review."));
            score += 20;
        }

        var decision = violations.Any(x => x.Severity == "block")
            ? "block"
            : violations.Any(x => x.Severity == "warn")
                ? "warn"
                : "allow";

        var result = new ExecutionReplayPolicyEvaluationResult(
            SourceExecutionSessionId: sourceExecutionSessionId,
            Scope: normalizedScope,
            GeneratedUtc: DateTimeOffset.UtcNow,
            Decision: decision,
            PolicyScore: Math.Clamp(score, 0, 100),
            Violations: violations,
            Metrics: new ExecutionReplayPolicyMetrics(
                replayDepth,
                preparedItemCount,
                totalWorkItems,
                failedWorkItems,
                deadLetteredWorkItems,
                activeReplayCount,
                deadLetteredPercent));

        await PersistEvaluationAsync(result, cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<ExecutionReplayPolicyEvaluationRecord>> ReadHistoryAsync(
        Guid sourceExecutionSessionId,
        int take,
        CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 250);
        var records = new List<ExecutionReplayPolicyEvaluationRecord>();
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@Take)
    ReplayPolicyEvaluationId,
    SourceExecutionSessionId,
    Scope,
    Decision,
    PolicyScore,
    MetricsJson,
    ViolationsJson,
    CreatedUtc
FROM dbo.MigrationExecutionReplayPolicyEvaluations
WHERE SourceExecutionSessionId = @SourceExecutionSessionId
ORDER BY CreatedUtc DESC;
";
        command.Parameters.AddWithValue("@SourceExecutionSessionId", sourceExecutionSessionId);
        command.Parameters.AddWithValue("@Take", safeTake);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new ExecutionReplayPolicyEvaluationRecord(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetFieldValue<DateTimeOffset>(7)));
        }

        return records;
    }

    private async Task PersistEvaluationAsync(
        ExecutionReplayPolicyEvaluationResult result,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO dbo.MigrationExecutionReplayPolicyEvaluations
(
    ReplayPolicyEvaluationId,
    SourceExecutionSessionId,
    Scope,
    Decision,
    PolicyScore,
    MetricsJson,
    ViolationsJson,
    CreatedUtc
)
VALUES
(
    @ReplayPolicyEvaluationId,
    @SourceExecutionSessionId,
    @Scope,
    @Decision,
    @PolicyScore,
    @MetricsJson,
    @ViolationsJson,
    SYSUTCDATETIME()
);
";
        command.Parameters.AddWithValue("@ReplayPolicyEvaluationId", Guid.NewGuid());
        command.Parameters.AddWithValue("@SourceExecutionSessionId", result.SourceExecutionSessionId);
        command.Parameters.AddWithValue("@Scope", result.Scope);
        command.Parameters.AddWithValue("@Decision", result.Decision);
        command.Parameters.AddWithValue("@PolicyScore", result.PolicyScore);
        command.Parameters.AddWithValue("@MetricsJson", JsonSerializer.Serialize(result.Metrics));
        command.Parameters.AddWithValue("@ViolationsJson", JsonSerializer.Serialize(result.Violations));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<int> ReadReplayDepthAsync(Guid executionSessionId, CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT ReplayDepth FROM dbo.MigrationExecutionSessions WHERE ExecutionSessionId = @ExecutionSessionId;";
        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    private async Task<int> CountActiveReplayChildrenAsync(Guid sourceExecutionSessionId, CancellationToken cancellationToken)
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

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
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
}
