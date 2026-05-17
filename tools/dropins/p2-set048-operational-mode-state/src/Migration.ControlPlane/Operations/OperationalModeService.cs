using Microsoft.Extensions.Hosting;

namespace Migration.ControlPlane.Operations;

public sealed class OperationalModeService : IOperationalModeService
{
    private readonly IHostEnvironment _environment;
    private readonly IProductionSafetyGateService _safetyGates;

    public OperationalModeService(
        IHostEnvironment environment,
        IProductionSafetyGateService safetyGates)
    {
        _environment = environment;
        _safetyGates = safetyGates;
    }

    public OperationalModeSnapshot GetSnapshot()
    {
        var safety = _safetyGates.GetSnapshot();
        var isLocal = _environment.IsDevelopment();
        var capabilities = new List<string>();
        var disabled = new List<string>();
        var warnings = new List<string>();

        capabilities.Add("diagnostics");
        capabilities.Add("readiness-rollups");
        capabilities.Add("audit-probes");
        capabilities.Add("telemetry-probes");

        if (safety.IsProductionReady)
        {
            capabilities.Add("production-ready");
        }
        else
        {
            disabled.Add("production-ready");
        }

        if (safety.IsLiveQueueExecutionAllowed)
        {
            capabilities.Add("live-queue-execution");
        }
        else
        {
            disabled.Add("live-queue-execution");
            warnings.Add("Live queue execution is disabled by current safety gates.");
        }

        if (isLocal)
        {
            warnings.Add("Environment is Development; local bypasses may be enabled.");
        }

        var mode =
            isLocal ? "local-development" :
            safety.IsLiveQueueExecutionAllowed ? "production-live-queue-ready" :
            safety.IsProductionReady ? "production-diagnostics-ready" :
            "diagnostics-only";

        return new OperationalModeSnapshot(
            GeneratedUtc: DateTimeOffset.UtcNow,
            EnvironmentName: _environment.EnvironmentName,
            Mode: mode,
            IsLocalDevelopment: isLocal,
            IsDiagnosticsOnly: !safety.IsLiveQueueExecutionAllowed,
            IsProductionReady: safety.IsProductionReady,
            IsLiveQueueExecutionAllowed: safety.IsLiveQueueExecutionAllowed,
            Capabilities: capabilities,
            DisabledCapabilities: disabled,
            Warnings: warnings);
    }
}
