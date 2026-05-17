namespace Migration.ControlPlane.Auth;

public sealed record AuthEnforcementDiagnostic(
    string Area,
    string RecommendedPolicy,
    bool EnforcementEnabled,
    bool ProductionBlocking,
    string Notes);

public sealed record AuthEnforcementDiagnosticsSnapshot(
    DateTimeOffset GeneratedUtc,
    bool GlobalAuthRequired,
    bool ProductionModeEnabled,
    IReadOnlyList<AuthEnforcementDiagnostic> Diagnostics,
    IReadOnlyList<string> Warnings);
