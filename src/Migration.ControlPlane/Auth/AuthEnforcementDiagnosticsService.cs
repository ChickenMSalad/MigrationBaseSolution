using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Migration.ControlPlane.Auth;

public sealed class AuthEnforcementDiagnosticsService : IAuthEnforcementDiagnosticsService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public AuthEnforcementDiagnosticsService(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public AuthEnforcementDiagnosticsSnapshot GetSnapshot()
    {
        var requiresAuth = ReadBool("Cloud:RequiresAuth", !_environment.IsDevelopment());
        var productionMode = !_environment.IsDevelopment();

        var diagnostics = new[]
        {
            Create("Projects", "AdminApi.Write", requiresAuth),
            Create("Runs", "AdminApi.Write", requiresAuth),
            Create("Credentials", "AdminApi.Credentials", requiresAuth),
            Create("Operations", "AdminApi.Operations", requiresAuth)
        };

        var warnings = new List<string>();

        if (!requiresAuth)
        {
            warnings.Add("Global auth enforcement is currently disabled.");
        }

        return new AuthEnforcementDiagnosticsSnapshot(
            DateTimeOffset.UtcNow,
            requiresAuth,
            productionMode,
            diagnostics,
            warnings);
    }

    private static AuthEnforcementDiagnostic Create(
        string area,
        string policy,
        bool enabled)
    {
        return new AuthEnforcementDiagnostic(
            area,
            policy,
            enabled,
            enabled,
            enabled
                ? "Auth enforcement readiness is enabled."
                : "Auth enforcement remains advisory only.");
    }

    private bool ReadBool(string key, bool fallback)
    {
        var value = _configuration[key];

        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }
}
