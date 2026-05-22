using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Migration.ControlPlane.Auth;

public sealed class CredentialAccessPolicyReadinessService : ICredentialAccessPolicyReadinessService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public CredentialAccessPolicyReadinessService(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public CredentialAccessPolicyReadinessSnapshot GetSnapshot()
    {
        var isDevelopment = _environment.IsDevelopment();
        var requiresAuth = ReadBool("Cloud:RequiresAuth", fallback: !isDevelopment);
        var allowsLocalDevelopmentBypass = isDevelopment && !requiresAuth;

        var requiresDedicatedCredentialScope = ReadBool(
            "CredentialAccessPolicy:RequiresDedicatedCredentialScope",
            fallback: true);

        var requiresAudit = ReadBool(
            "CredentialAccessPolicy:RequiresAudit",
            fallback: true);

        var requiresTelemetry = ReadBool(
            "CredentialAccessPolicy:RequiresTelemetry",
            fallback: true);

        var blocking = new List<string>();
        var warnings = new List<string>();

        if (!isDevelopment && !requiresAuth)
        {
            blocking.Add("Credential access cannot be production-ready while auth is disabled.");
        }

        if (!requiresDedicatedCredentialScope)
        {
            blocking.Add("Credential access should require a dedicated credential scope.");
        }

        if (!requiresAudit)
        {
            warnings.Add("Credential access audit is disabled.");
        }

        if (!requiresTelemetry)
        {
            warnings.Add("Credential access telemetry is disabled.");
        }

        if (allowsLocalDevelopmentBypass)
        {
            warnings.Add("Credential access auth bypass is allowed only because the environment is Development and auth is not required.");
        }

        return new CredentialAccessPolicyReadinessSnapshot(
            GeneratedUtc: DateTimeOffset.UtcNow,
            RequiresAuth: requiresAuth,
            IsDevelopment: isDevelopment,
            AllowsLocalDevelopmentBypass: allowsLocalDevelopmentBypass,
            RequiresDedicatedCredentialScope: requiresDedicatedCredentialScope,
            RequiresAuditForCredentialAccess: requiresAudit,
            RequiresTelemetryForCredentialAccess: requiresTelemetry,
            IsReadyForProduction: blocking.Count == 0,
            Requirements: BuildRequirements(allowsLocalDevelopmentBypass),
            BlockingIssues: blocking,
            Warnings: warnings);
    }

    private static IReadOnlyList<CredentialAccessPolicyRequirement> BuildRequirements(
        bool allowsLocalDevelopmentBypass) =>
    [
        new(
            Operation: "credential.metadata.read",
            RequiredPolicy: "AdminApi.Credentials",
            RequiredScope: "admin.credentials",
            RequiresAudit: true,
            RequiresTelemetry: true,
            AllowedInDevelopmentWithoutAuth: allowsLocalDevelopmentBypass,
            Description: "Read credential metadata and provider planning information."),
        new(
            Operation: "credential.binding.write",
            RequiredPolicy: "AdminApi.Credentials",
            RequiredScope: "admin.credentials",
            RequiresAudit: true,
            RequiresTelemetry: true,
            AllowedInDevelopmentWithoutAuth: allowsLocalDevelopmentBypass,
            Description: "Create or update project/run credential bindings."),
        new(
            Operation: "credential.value.probe",
            RequiredPolicy: "AdminApi.Credentials",
            RequiredScope: "admin.credentials",
            RequiresAudit: true,
            RequiresTelemetry: true,
            AllowedInDevelopmentWithoutAuth: allowsLocalDevelopmentBypass,
            Description: "Probe credential value existence without returning secret values."),
        new(
            Operation: "credential.secret.resolve",
            RequiredPolicy: "AdminApi.Credentials",
            RequiredScope: "admin.credentials",
            RequiresAudit: true,
            RequiresTelemetry: true,
            AllowedInDevelopmentWithoutAuth: false,
            Description: "Resolve secret material for runtime use only; should not be exposed through diagnostics.")
    ];

    private bool ReadBool(string key, bool fallback)
    {
        var value = _configuration[key];

        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }
}
