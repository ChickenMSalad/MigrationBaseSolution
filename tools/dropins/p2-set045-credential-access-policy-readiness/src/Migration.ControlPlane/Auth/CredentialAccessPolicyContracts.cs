namespace Migration.ControlPlane.Auth;

public sealed record CredentialAccessPolicyRequirement(
    string Operation,
    string RequiredPolicy,
    string RequiredScope,
    bool RequiresAudit,
    bool RequiresTelemetry,
    bool AllowedInDevelopmentWithoutAuth,
    string Description);

public sealed record CredentialAccessPolicyReadinessSnapshot(
    DateTimeOffset GeneratedUtc,
    bool RequiresAuth,
    bool IsDevelopment,
    bool AllowsLocalDevelopmentBypass,
    bool RequiresDedicatedCredentialScope,
    bool RequiresAuditForCredentialAccess,
    bool RequiresTelemetryForCredentialAccess,
    bool IsReadyForProduction,
    IReadOnlyList<CredentialAccessPolicyRequirement> Requirements,
    IReadOnlyList<string> BlockingIssues,
    IReadOnlyList<string> Warnings);
