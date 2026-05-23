namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionReplayPreparationService : IExecutionReplayPreparationService
{
    private readonly IExecutionDiagnosticExportService _exportService;
    private readonly IExecutionReplayAnalysisService _analysisService;

    public SqlExecutionReplayPreparationService(
        IExecutionDiagnosticExportService exportService,
        IExecutionReplayAnalysisService analysisService)
    {
        _exportService = exportService;
        _analysisService = analysisService;
    }

    public async Task<ExecutionReplayPreparationResult> PrepareAsync(
        PrepareExecutionReplayRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedScope = NormalizeScope(request.Scope);
        var bundle = await _exportService.BuildBundleAsync(request.ExecutionSessionId, cancellationToken);
        var analysis = await _analysisService.AnalyzeAsync(request.ExecutionSessionId, cancellationToken);

        var findings = analysis.Findings.ToList();

        if (bundle.Session is null)
        {
            return new ExecutionReplayPreparationResult(
                SourceExecutionSessionId: request.ExecutionSessionId,
                GeneratedUtc: DateTimeOffset.UtcNow,
                Scope: normalizedScope,
                RequiresApproval: true,
                CanPrepareReplay: false,
                Recommendation: "Replay cannot be prepared because the source execution session was not found.",
                Items: Array.Empty<ExecutionReplayPreparationItem>(),
                Findings: findings);
        }

        var candidates = SelectCandidates(bundle.WorkItems, normalizedScope).ToArray();

        if (candidates.Length == 0)
        {
            findings.Add(new ExecutionReplayFinding(
                Severity: "warning",
                Code: "replay-scope-empty",
                Message: $"No work items matched replay scope '{normalizedScope}'."));
        }

        if (analysis.RiskScore >= 75)
        {
            findings.Add(new ExecutionReplayFinding(
                Severity: "critical",
                Code: "replay-risk-too-high",
                Message: "Replay preparation is blocked because replay-readiness risk is too high."));
        }

        var canPrepareReplay = candidates.Length > 0 && analysis.RiskScore < 75;

        var items = candidates
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedUtc)
            .Select((item, index) => new ExecutionReplayPreparationItem(
                SourceExecutionWorkItemId: item.ExecutionWorkItemId,
                SourceExecutionPlanStepId: item.ExecutionPlanStepId,
                ReplayOrder: index + 1,
                ReplayType: item.WorkItemType,
                ReplayName: item.WorkItemName,
                SourceStatus: item.Status,
                PayloadJson: item.PayloadJson))
            .ToArray();

        var recommendation = canPrepareReplay
            ? $"Replay manifest prepared with {items.Length} item(s). Review and approve before creating a replay execution session."
            : "Replay manifest could not be safely prepared. Resolve critical findings first.";

        return new ExecutionReplayPreparationResult(
            SourceExecutionSessionId: request.ExecutionSessionId,
            GeneratedUtc: DateTimeOffset.UtcNow,
            Scope: normalizedScope,
            RequiresApproval: true,
            CanPrepareReplay: canPrepareReplay,
            Recommendation: recommendation,
            Items: items,
            Findings: findings);
    }

    private static IEnumerable<ExecutionWorkItemRecord> SelectCandidates(
        IReadOnlyList<ExecutionWorkItemRecord> workItems,
        string scope)
    {
        return scope switch
        {
            "failed-only" => workItems.Where(x => x.Status == "failed"),
            "dead-letter-only" => workItems.Where(x => x.Status == "dead-lettered"),
            "incomplete-only" => workItems.Where(x => x.Status is "pending" or "leased" or "running" or "failed" or "dead-lettered" or "cancelled"),
            "all" => workItems,
            _ => workItems.Where(x => x.Status is "failed" or "dead-lettered")
        };
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
