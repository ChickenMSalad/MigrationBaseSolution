using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Migration.ControlPlane.Auth;

public sealed class AuthPolicyReadinessService : IAuthPolicyReadinessService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public AuthPolicyReadinessService(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public AuthPolicyReadinessSnapshot GetSnapshot()
    {
        var environmentName = _environment.EnvironmentName;
        var isDevelopment = _environment.IsDevelopment();
        var requiresAuth = ReadBool("Cloud:RequiresAuth", fallback: !isDevelopment);
        var isProductionLike = !isDevelopment || requiresAuth;

        var policies = BuildPolicies();
        var blocking = new List<string>();
        var warnings = new List<string>();

        if (isProductionLike && !requiresAuth)
        {
            blocking.Add("Production-like environment does not require auth.");
        }

        if (isDevelopment && !requiresAuth)
        {
            warnings.Add("Auth is disabled for local development.");
        }

        var configuredAuthMode = FirstNonEmpty(
            _configuration["Authentication:Mode"],
            _configuration["AdminApi:Authentication:Mode"],
            requiresAuth ? "required" : "disabled");

        if (isProductionLike &&
            configuredAuthMode.Equals("disabled", StringComparison.OrdinalIgnoreCase))
        {
            blocking.Add("Authentication mode is disabled in a production-like environment.");
        }

        var ready = blocking.Count == 0;

        return new AuthPolicyReadinessSnapshot(
            GeneratedUtc: DateTimeOffset.UtcNow,
            EnvironmentName: environmentName,
            RequiresAuth: requiresAuth,
            IsDevelopment: isDevelopment,
            IsProductionLike: isProductionLike,
            IsReadyForProduction: ready,
            RequiredPolicies: policies,
            BlockingIssues: blocking,
            Warnings: warnings);
    }

    private static IReadOnlyList<AuthPolicyRequirement> BuildPolicies() =>
    [
        new(
            PolicyName: "AdminApi.Read",
            Scope: "admin.read",
            RequiredInProduction: true,
            RequiredInDevelopment: false,
            Description: "Allows read-only Admin API diagnostics and status endpoints."),
        new(
            PolicyName: "AdminApi.Write",
            Scope: "admin.write",
            RequiredInProduction: true,
            RequiredInDevelopment: false,
            Description: "Allows mutating Admin API actions."),
        new(
            PolicyName: "AdminApi.Credentials",
            Scope: "admin.credentials",
            RequiredInProduction: true,
            RequiredInDevelopment: false,
            Description: "Allows credential metadata and secret existence operations."),
        new(
            PolicyName: "AdminApi.Operations",
            Scope: "admin.operations",
            RequiredInProduction: true,
            RequiredInDevelopment: false,
            Description: "Allows operational diagnostics, readiness, queue, audit, and telemetry operations.")
    ];

    private bool ReadBool(string key, bool fallback)
    {
        var value = _configuration[key];

        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
