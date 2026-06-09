namespace Migration.Admin.Api.Operational.Execution;

public sealed record ExecutionReplayAnalysisResult(
    Guid ExecutionSessionId,
    DateTimeOffset GeneratedUtc,
    bool ReplayRecommended,
    string Recommendation,
    int RiskScore,
    IReadOnlyList<ExecutionReplayFinding> Findings,
    ExecutionReplayStateSummary StateSummary);

public sealed record ExecutionReplayFinding(
    string Severity,
    string Code,
    string Message);

public sealed record ExecutionReplayStateSummary(
    string? SessionStatus,
    int PlanStepCount,
    int WorkItemCount,
    int PendingWorkItems,
    int LeasedWorkItems,
    int CompletedWorkItems,
    int FailedWorkItems,
    int DeadLetteredWorkItems,
    int CancelledWorkItems,
    int OperationalEventCount,
    int PhaseTransitionCount);


