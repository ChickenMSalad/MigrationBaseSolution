namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionReplayAnalysisService : IExecutionReplayAnalysisService
{
    private readonly IExecutionDiagnosticExportService _exportService;

    public SqlExecutionReplayAnalysisService(IExecutionDiagnosticExportService exportService)
    {
        _exportService = exportService;
    }

    public async Task<ExecutionReplayAnalysisResult> AnalyzeAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken)
    {
        var bundle = await _exportService.BuildBundleAsync(executionSessionId, cancellationToken);

        if (bundle.Session is null)
        {
            return new ExecutionReplayAnalysisResult(
                ExecutionSessionId: executionSessionId,
                GeneratedUtc: DateTimeOffset.UtcNow,
                ReplayRecommended: false,
                Recommendation: "Execution session was not found.",
                RiskScore: 100,
                Findings:
                [
                    new ExecutionReplayFinding("critical", "session-not-found", "The requested execution session does not exist.")
                ],
                StateSummary: new ExecutionReplayStateSummary(null, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        }

        var findings = new List<ExecutionReplayFinding>();
        var riskScore = 0;

        AddStatusFindings(bundle.Session.Status, findings, ref riskScore);
        AddPlanFindings(bundle.PlanSteps, findings, ref riskScore);
        AddWorkItemFindings(bundle.WorkItems, findings, ref riskScore);
        AddOperationalEventFindings(bundle.OperationalEvents, findings, ref riskScore);
        AddWorkerFindings(bundle.WorkerTelemetry, findings, ref riskScore);

        riskScore = Math.Clamp(riskScore, 0, 100);

        var replayRecommended =
            riskScore < 40 &&
            bundle.PlanSteps.Count > 0 &&
            bundle.WorkItems.All(x => x.Status is "completed" or "cancelled" or "dead-lettered" or "failed" or "pending");

        var recommendation = replayRecommended
            ? "Replay appears feasible. Review warnings, then create a new execution session from the same source/target intent."
            : riskScore >= 75
                ? "Replay is high risk. Resolve critical findings before attempting replay."
                : "Replay needs operator review. Resolve warnings or export diagnostics before replay.";

        return new ExecutionReplayAnalysisResult(
            ExecutionSessionId: executionSessionId,
            GeneratedUtc: DateTimeOffset.UtcNow,
            ReplayRecommended: replayRecommended,
            Recommendation: recommendation,
            RiskScore: riskScore,
            Findings: findings,
            StateSummary: new ExecutionReplayStateSummary(
                SessionStatus: bundle.Session.Status,
                PlanStepCount: bundle.PlanSteps.Count,
                WorkItemCount: bundle.WorkItems.Count,
                PendingWorkItems: bundle.WorkItems.Count(x => x.Status == "pending"),
                LeasedWorkItems: bundle.WorkItems.Count(x => x.Status == "leased"),
                CompletedWorkItems: bundle.WorkItems.Count(x => x.Status == "completed"),
                FailedWorkItems: bundle.WorkItems.Count(x => x.Status == "failed"),
                DeadLetteredWorkItems: bundle.WorkItems.Count(x => x.Status == "dead-lettered"),
                CancelledWorkItems: bundle.WorkItems.Count(x => x.Status == "cancelled"),
                OperationalEventCount: bundle.OperationalEvents.Count,
                PhaseTransitionCount: bundle.PhaseHistory.Count));
    }

    private static void AddStatusFindings(string status, List<ExecutionReplayFinding> findings, ref int riskScore)
    {
        if (status == "running")
        {
            riskScore += 35;
            findings.Add(new ExecutionReplayFinding("warning", "session-running", "The execution session is currently running. Replay should wait until it reaches a terminal state or is paused/cancelled."));
        }

        if (status == "paused")
        {
            riskScore += 10;
            findings.Add(new ExecutionReplayFinding("info", "session-paused", "The execution session is paused. Replay can be evaluated, but the current session still has recoverable state."));
        }

        if (status == "cancelled")
        {
            riskScore += 15;
            findings.Add(new ExecutionReplayFinding("warning", "session-cancelled", "The execution session was cancelled. Replay may be appropriate, but cancelled work should be reviewed."));
        }

        if (status == "completed")
        {
            findings.Add(new ExecutionReplayFinding("info", "session-completed", "The execution session completed. Replay should usually be treated as a controlled rerun, not recovery."));
        }
    }

    private static void AddPlanFindings(IReadOnlyList<ExecutionPlanStepRecord> planSteps, List<ExecutionReplayFinding> findings, ref int riskScore)
    {
        if (planSteps.Count == 0)
        {
            riskScore += 35;
            findings.Add(new ExecutionReplayFinding("critical", "plan-missing", "No execution plan steps were found. Replay cannot be reconstructed deterministically."));
            return;
        }

        var duplicateOrders = planSteps
            .GroupBy(x => x.StepOrder)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();

        if (duplicateOrders.Length > 0)
        {
            riskScore += 25;
            findings.Add(new ExecutionReplayFinding("critical", "plan-duplicate-step-order", $"Execution plan contains duplicate step order values: {string.Join(", ", duplicateOrders)}."));
        }

        var missingConnectorSteps = planSteps.Count(x =>
            string.IsNullOrWhiteSpace(x.SourceConnector) ||
            string.IsNullOrWhiteSpace(x.TargetConnector));

        if (missingConnectorSteps > 0)
        {
            riskScore += 10;
            findings.Add(new ExecutionReplayFinding("warning", "plan-missing-connectors", $"{missingConnectorSteps} plan step(s) do not have complete source/target connector labels."));
        }
    }

    private static void AddWorkItemFindings(IReadOnlyList<ExecutionWorkItemRecord> workItems, List<ExecutionReplayFinding> findings, ref int riskScore)
    {
        if (workItems.Count == 0)
        {
            riskScore += 30;
            findings.Add(new ExecutionReplayFinding("warning", "work-items-missing", "No execution work items were found. Replay can only reconstruct plan intent, not queue progress."));
            return;
        }

        var leased = workItems.Count(x => x.Status == "leased");
        if (leased > 0)
        {
            riskScore += 25;
            findings.Add(new ExecutionReplayFinding("warning", "leased-work-items", $"{leased} work item(s) are still leased. Requeue or cancel before replay."));
        }

        var deadLettered = workItems.Count(x => x.Status == "dead-lettered");
        if (deadLettered > 0)
        {
            riskScore += 30;
            findings.Add(new ExecutionReplayFinding("critical", "dead-lettered-work-items", $"{deadLettered} work item(s) are dead-lettered. Review failures before replay."));
        }

        var failed = workItems.Count(x => x.Status == "failed");
        if (failed > 0)
        {
            riskScore += 15;
            findings.Add(new ExecutionReplayFinding("warning", "failed-work-items", $"{failed} work item(s) are failed but may still be recoverable."));
        }

        var completed = workItems.Count(x => x.Status == "completed");
        if (completed > 0)
        {
            riskScore += 5;
            findings.Add(new ExecutionReplayFinding("info", "completed-work-items", $"{completed} completed work item(s) exist. Replay should account for idempotency."));
        }
    }

    private static void AddOperationalEventFindings(IReadOnlyList<Events.OperationalEventRecord> events, List<ExecutionReplayFinding> findings, ref int riskScore)
    {
        if (events.Count == 0)
        {
            riskScore += 20;
            findings.Add(new ExecutionReplayFinding("warning", "events-missing", "No correlated operational events were found. Replay forensics are incomplete."));
        }

        var criticalEvents = events.Count(x => x.Severity == "critical");
        if (criticalEvents > 0)
        {
            riskScore += 20;
            findings.Add(new ExecutionReplayFinding("critical", "critical-events", $"{criticalEvents} critical operational event(s) are correlated to this session."));
        }
    }

    private static void AddWorkerFindings(ExecutionWorkerTelemetrySummary? workerTelemetry, List<ExecutionReplayFinding> findings, ref int riskScore)
    {
        if (workerTelemetry is null)
        {
            riskScore += 10;
            findings.Add(new ExecutionReplayFinding("warning", "worker-telemetry-missing", "Worker telemetry was not available for replay analysis."));
            return;
        }

        if (workerTelemetry.StaleWorkers > 0)
        {
            riskScore += 10;
            findings.Add(new ExecutionReplayFinding("warning", "stale-workers", $"{workerTelemetry.StaleWorkers} stale worker heartbeat(s) were observed."));
        }
    }
}


