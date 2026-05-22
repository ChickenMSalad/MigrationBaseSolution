namespace Migration.ControlPlane.Operations;

public sealed record OperationalModeSnapshot(
    DateTimeOffset GeneratedUtc,
    string EnvironmentName,
    string Mode,
    bool IsLocalDevelopment,
    bool IsDiagnosticsOnly,
    bool IsProductionReady,
    bool IsLiveQueueExecutionAllowed,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> DisabledCapabilities,
    IReadOnlyList<string> Warnings);
