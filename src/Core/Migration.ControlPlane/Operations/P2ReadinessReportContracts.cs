namespace Migration.ControlPlane.Operations;

public sealed record P2ReadinessReportSnapshot(
    DateTimeOffset GeneratedUtc,
    string OverallStatus,
    bool IsDiagnosticsReady,
    bool IsProductionReady,
    bool IsLiveQueueExecutionReady,
    string OperationalMode,
    IReadOnlyList<string> CompletedCapabilityAreas,
    IReadOnlyList<string> RemainingRecommendedAreas,
    IReadOnlyList<string> Warnings);
