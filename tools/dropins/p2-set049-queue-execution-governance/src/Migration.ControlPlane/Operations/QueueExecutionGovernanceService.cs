namespace Migration.ControlPlane.Operations;

public sealed class QueueExecutionGovernanceService : IQueueExecutionGovernanceService
{
    private readonly IProductionSafetyGateService _safetyGates;
    private readonly IOperationalModeService _operationalMode;

    public QueueExecutionGovernanceService(
        IProductionSafetyGateService safetyGates,
        IOperationalModeService operationalMode)
    {
        _safetyGates = safetyGates;
        _operationalMode = operationalMode;
    }

    public QueueExecutionGovernanceDecision GetDecision()
    {
        var safety = _safetyGates.GetSnapshot();
        var mode = _operationalMode.GetSnapshot();

        var required = new List<string>
        {
            "production safety gates must pass",
            "operational readiness must pass",
            "queue execution readiness must pass",
            "audit provider should be durable",
            "telemetry provider must be configured",
            "message completion must be explicitly enabled",
            "manual approval should be recorded before live queue execution"
        };

        var blocking = new List<string>();
        var warnings = new List<string>();

        if (!safety.IsProductionReady)
        {
            blocking.Add("Production safety gates have not passed.");
        }

        if (!safety.IsLiveQueueExecutionAllowed)
        {
            blocking.Add("Live queue execution is not allowed by safety gates.");
        }

        if (!mode.IsLiveQueueExecutionAllowed)
        {
            blocking.Add("Operational mode does not allow live queue execution.");
        }

        if (mode.IsLocalDevelopment)
        {
            warnings.Add("Current environment is local development.");
        }

        foreach (var issue in safety.BlockingIssues)
        {
            AddUnique(blocking, issue);
        }

        foreach (var warning in safety.Warnings)
        {
            AddUnique(warnings, warning);
        }

        foreach (var warning in mode.Warnings)
        {
            AddUnique(warnings, warning);
        }

        var canEnable = blocking.Count == 0;

        return new QueueExecutionGovernanceDecision(
            GeneratedUtc: DateTimeOffset.UtcNow,
            CanEnableLiveQueueExecution: canEnable,
            CanCompleteMessages: canEnable,
            RequiresManualApproval: true,
            RecommendedMode: canEnable ? "production-live-queue-ready" : "diagnostics-only",
            RequiredConditions: required,
            BlockingIssues: blocking,
            Warnings: warnings);
    }

    private static void AddUnique(List<string> values, string value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            !values.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            values.Add(value);
        }
    }
}
