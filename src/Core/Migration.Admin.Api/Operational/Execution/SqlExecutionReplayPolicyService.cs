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
            violations.Add(new ExecutionReplayPolicyViolation(
                "block",
                "source-session-missing",
                "The source execution session does not exist."));
            score += 100;
        }

        if (replayDepth >= MaxReplayDepth)
        {
            violations.Add(new ExecutionReplayPolicyViolation(
                "block",
                "replay-depth-limit",
                $"Replay depth {replayDepth} meets or exceeds the maximum depth of {MaxReplayDepth}."));
            score += 100;
        }
        else if (replayDepth == MaxReplayDepth - 1)
        {
            violations.Add(new ExecutionReplayPolicyViolation(
                "warn",
                "replay-depth-near-limit",
                $"Replay depth {replayDepth} is one level below the maximum depth of {MaxReplayDepth}."));
            score += 20;
        }

        if (activeReplayCount > 0)
        {
            violations.Add(new ExecutionReplayPolicyViolation(
                "block",
                "active-replay-conflict",
                $"{activeReplayCount} active replay session(s) already exist for this source session."));
            score += 100;
        }

        if (preparedItemCount == 0)
        {
            violations.Add(new ExecutionReplayPolicyViolation(
                "block",
                "empty-replay-manifest",
                "The selected scope produced no replay items."));
            score += 80;
        }
        else if (preparedItemCount > BlockPreparedItemCount)
        {
            violations.Add(new ExecutionReplayPolicyViolation(
                "block",
                "replay-volume-too-large",
                $"Replay manifest contains {preparedItemCount} items, above the block threshold of {BlockPreparedItemCount}."));
            score += 100;
        }
        else if (preparedItemCount > WarnPreparedItemCount)
        {
            violations.Add(new ExecutionReplayPolicyViolation(
                "warn",
                "replay-volume-large",
                $"Replay manifest contains {preparedItemCount} items, above the warning threshold of {WarnPreparedItemCount}."));
            score += 25;
        }

        if (deadLetteredPercent >= BlockDeadLetteredPercent)
        {
            violations.Add(new ExecutionReplayPolicyViolation(
                "block",
                "dead-letter-pressure-high",
                $"Dead-lettered work item percentage is {deadLetteredPercent}%, above the block threshold of {BlockDeadLetteredPercent}%."));
            score += 100;
        }
        else if (deadLetteredPercent >= WarnDeadLetteredPercent)
        {
            violations.Add(new ExecutionReplayPolicyViolation(
                "warn",
                "dead-letter-pressure-elevated",
                $"Dead-lettered work item percentage is {deadLetteredPercent}%, above the warning threshold of {WarnDeadLetteredPercent}%."));
            score += 30;
        }

        if (normalizedScope == "all")
        {
            violations.Add(new ExecutionReplayPolicyViolation(
                "warn",
                "all-scope-replay",
                "Replay scope 'all' can duplicate completed work and requires extra idempotency review."));
            score += 20;
        }

        var decision = violations.Any(x => x.Severity == "block")
            ? "block"
            : violations.Any(x => x.Severity == "warn")
                ? "warn"
                : "allow";

        return new ExecutionReplayPolicyEvaluationResult(
            SourceExecutionSessionId: sourceExecutionSessionId,
            Scope: normalizedScope,
            GeneratedUtc: DateTimeOffset.UtcNow,
            Decision: decision,
            PolicyScore: Math.Clamp(score, 0, 100),
            Violations: violations,
            Metrics: new ExecutionReplayPolicyMetrics(
                ReplayDepth: replayDepth,
                PreparedItemCount: preparedItemCount,
                TotalWorkItemCount: totalWorkItems,
                FailedWorkItemCount: failedWorkItems,
                DeadLetteredWorkItemCount: deadLetteredWorkItems,
                ActiveReplayCount: activeReplayCount,
                DeadLetteredPercent: deadLetteredPercent));
    }

    private async Task<int> ReadReplayDepthAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ReplayDepth
FROM dbo.MigrationExecutionSessions
WHERE ExecutionSessionId = @ExecutionSessionId;
";

        command.Parameters.AddWithValue("@ExecutionSessionId", executionSessionId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    private async Task<int> CountActiveReplayChildrenAsync(
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
