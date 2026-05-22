using Migration.ControlPlane.Auth;

namespace Migration.ControlPlane.Operations;

public sealed record ProductionSafetyGate(
    string Name,
    bool Passed,
    bool RequiredForProduction,
    string Description,
    IReadOnlyList<string> Issues);

public sealed record ProductionSafetyGateSnapshot(
    DateTimeOffset GeneratedUtc,
    bool IsProductionReady,
    bool IsLiveQueueExecutionAllowed,
    IReadOnlyList<ProductionSafetyGate> Gates,
    IReadOnlyList<string> BlockingIssues,
    IReadOnlyList<string> Warnings,
    AuthPolicyReadinessSnapshot AuthPolicy,
    CredentialAccessPolicyReadinessSnapshot CredentialAccess,
    OperationalReadinessSnapshot OperationalReadiness);
