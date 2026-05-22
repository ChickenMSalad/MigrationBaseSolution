namespace Migration.ControlPlane.Operations;

public sealed record QueueExecutionGovernanceDecision(
    DateTimeOffset GeneratedUtc,
    bool CanEnableLiveQueueExecution,
    bool CanCompleteMessages,
    bool RequiresManualApproval,
    string RecommendedMode,
    IReadOnlyList<string> RequiredConditions,
    IReadOnlyList<string> BlockingIssues,
    IReadOnlyList<string> Warnings);
