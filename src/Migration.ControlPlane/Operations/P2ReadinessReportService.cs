namespace Migration.ControlPlane.Operations;

public sealed class P2ReadinessReportService : IP2ReadinessReportService
{
    private readonly IProductionSafetyGateService _safety;
    private readonly IOperationalModeService _mode;
    private readonly IQueueExecutionGovernanceService _governance;

    public P2ReadinessReportService(
        IProductionSafetyGateService safety,
        IOperationalModeService mode,
        IQueueExecutionGovernanceService governance)
    {
        _safety = safety;
        _mode = mode;
        _governance = governance;
    }

    public P2ReadinessReportSnapshot GetSnapshot()
    {
        var safety = _safety.GetSnapshot();
        var mode = _mode.GetSnapshot();
        var governance = _governance.GetDecision();

        var completed = new[]
        {
            "workspace storage planning",
            "artifact storage planning",
            "credential planning",
            "queue execution diagnostics",
            "audit persistence diagnostics",
            "telemetry diagnostics",
            "operational readiness rollups",
            "auth policy readiness",
            "endpoint policy inventory",
            "credential access policy readiness",
            "production safety gates",
            "operational mode state",
            "queue execution governance"
        };

        var remaining = new[]
        {
            "optional production auth enforcement rollout",
            "optional live queue execution enablement",
            "optional P3 execution orchestration"
        };

        var warnings = governance.Warnings
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var status =
            governance.CanEnableLiveQueueExecution ? "production-live-ready" :
            safety.IsProductionReady ? "production-diagnostics-ready" :
            "diagnostics-ready";

        return new P2ReadinessReportSnapshot(
            GeneratedUtc: DateTimeOffset.UtcNow,
            OverallStatus: status,
            IsDiagnosticsReady: true,
            IsProductionReady: safety.IsProductionReady,
            IsLiveQueueExecutionReady: governance.CanEnableLiveQueueExecution,
            OperationalMode: mode.Mode,
            CompletedCapabilityAreas: completed,
            RemainingRecommendedAreas: remaining,
            Warnings: warnings);
    }
}
