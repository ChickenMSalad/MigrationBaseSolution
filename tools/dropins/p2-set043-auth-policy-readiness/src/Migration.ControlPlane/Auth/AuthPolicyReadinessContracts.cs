namespace Migration.ControlPlane.Auth;

public sealed record AuthPolicyRequirement(
    string PolicyName,
    string Scope,
    bool RequiredInProduction,
    bool RequiredInDevelopment,
    string Description);

public sealed record AuthPolicyReadinessSnapshot(
    DateTimeOffset GeneratedUtc,
    string EnvironmentName,
    bool RequiresAuth,
    bool IsDevelopment,
    bool IsProductionLike,
    bool IsReadyForProduction,
    IReadOnlyList<AuthPolicyRequirement> RequiredPolicies,
    IReadOnlyList<string> BlockingIssues,
    IReadOnlyList<string> Warnings);
