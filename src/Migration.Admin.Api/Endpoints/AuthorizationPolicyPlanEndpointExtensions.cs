using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class AuthorizationPolicyPlanEndpointExtensions
{
    public static RouteGroupBuilder MapAuthorizationPolicyPlanEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/auth/policy-plan", (
                IConfiguration configuration,
                IWebHostEnvironment environment) =>
            {
                var descriptor = BuildDescriptor(configuration, environment);
                return Results.Ok(descriptor);
            })
            .WithName("GetAuthorizationPolicyPlan")
            .WithTags("Cloud")
            .WithSummary("Gets the planned authorization policy shape for cloud auth enforcement.")
            .Produces<AuthorizationPolicyPlanDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static AuthorizationPolicyPlanDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var authMode = Read(
            configuration,
            "Cloud:AuthMode",
            environment.IsDevelopment() ? "disabled" : "entraId");

        var authRequired = ReadBool(
            configuration,
            "Cloud:RequiresAuth",
            !environment.IsDevelopment());

        var tenantEnforced = ReadBool(
            configuration,
            "Workspace:TenantEnforced",
            false);

        var authority = FirstNonEmptyOrNull(configuration["Auth:Authority"]);
        var audience = FirstNonEmptyOrNull(configuration["Auth:Audience"]);

        var warnings = BuildWarnings(
            environment,
            authMode,
            authRequired,
            tenantEnforced,
            authority,
            audience);

        return new AuthorizationPolicyPlanDescriptor(
            EnvironmentName: environment.EnvironmentName,
            AuthMode: authMode,
            AuthRequired: authRequired,
            TenantEnforced: tenantEnforced,
            Authority: authority is null ? null : "<configured>",
            Audience: audience is null ? null : "<configured>",
            Roles: BuildRoles(),
            Scopes: BuildScopes(),
            RoutePolicies: BuildRoutePolicies(),
            Warnings: warnings);
    }

    private static IReadOnlyList<AuthorizationRoleDescriptor> BuildRoles() =>
    [
        new(
            AuthorizationRoleNames.Reader,
            "Reader",
            "Can view projects, runs, artifacts, connector metadata, and cloud diagnostics."),

        new(
            AuthorizationRoleNames.Operator,
            "Operator",
            "Can queue runs, cancel runs, upload artifacts, and manage migration execution."),

        new(
            AuthorizationRoleNames.Admin,
            "Admin",
            "Can manage projects, credentials, mappings, connector configuration, and cloud settings."),

        new(
            AuthorizationRoleNames.Auditor,
            "Auditor",
            "Can view audit, readiness, and operational diagnostics.")
    ];

    private static IReadOnlyList<AuthorizationScopeDescriptor> BuildScopes() =>
    [
        new(AuthorizationScopeNames.Read, "Read", "Read project, run, artifact, connector, and diagnostic data."),
        new(AuthorizationScopeNames.Write, "Write", "Create and update projects, artifacts, mappings, and credentials."),
        new(AuthorizationScopeNames.Execute, "Execute", "Queue and cancel preflight/migration runs."),
        new(AuthorizationScopeNames.Admin, "Admin", "Manage administrative settings and privileged resources."),
        new(AuthorizationScopeNames.Audit, "Audit", "Read audit and compliance-oriented records.")
    ];

    private static IReadOnlyList<AuthorizationRoutePolicyDescriptor> BuildRoutePolicies() =>
    [
        new(
            "/api/cloud/*",
            "CloudDiagnosticsRead",
            [AuthorizationRoleNames.Admin, AuthorizationRoleNames.Auditor],
            [AuthorizationScopeNames.Read, AuthorizationScopeNames.Audit]),

        new(
            "/api/workspace/*",
            "WorkspaceRead",
            [AuthorizationRoleNames.Reader, AuthorizationRoleNames.Operator, AuthorizationRoleNames.Admin],
            [AuthorizationScopeNames.Read]),

        new(
            "/api/connectors/*",
            "ConnectorCatalogRead",
            [AuthorizationRoleNames.Reader, AuthorizationRoleNames.Operator, AuthorizationRoleNames.Admin],
            [AuthorizationScopeNames.Read]),

        new(
            "/api/projects GET",
            "ProjectRead",
            [AuthorizationRoleNames.Reader, AuthorizationRoleNames.Operator, AuthorizationRoleNames.Admin],
            [AuthorizationScopeNames.Read]),

        new(
            "/api/projects POST|PUT|DELETE",
            "ProjectWrite",
            [AuthorizationRoleNames.Admin],
            [AuthorizationScopeNames.Write]),

        new(
            "/api/projects/{projectId}/preflight",
            "RunExecute",
            [AuthorizationRoleNames.Operator, AuthorizationRoleNames.Admin],
            [AuthorizationScopeNames.Execute]),

        new(
            "/api/projects/{projectId}/runs",
            "RunExecute",
            [AuthorizationRoleNames.Operator, AuthorizationRoleNames.Admin],
            [AuthorizationScopeNames.Execute]),

        new(
            "/api/runs/*",
            "RunReadExecute",
            [AuthorizationRoleNames.Reader, AuthorizationRoleNames.Operator, AuthorizationRoleNames.Admin],
            [AuthorizationScopeNames.Read, AuthorizationScopeNames.Execute]),

        new(
            "/api/credentials/*",
            "CredentialAdmin",
            [AuthorizationRoleNames.Admin],
            [AuthorizationScopeNames.Admin]),

        new(
            "/api/artifacts/*",
            "ArtifactWrite",
            [AuthorizationRoleNames.Operator, AuthorizationRoleNames.Admin],
            [AuthorizationScopeNames.Write]),

        new(
            "/health/*",
            "HealthRead",
            [AuthorizationRoleNames.Reader, AuthorizationRoleNames.Operator, AuthorizationRoleNames.Admin, AuthorizationRoleNames.Auditor],
            [AuthorizationScopeNames.Read])
    ];

    private static List<string> BuildWarnings(
        IWebHostEnvironment environment,
        string authMode,
        bool authRequired,
        bool tenantEnforced,
        string? authority,
        string? audience)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() && !authRequired)
        {
            warnings.Add("Authentication should be required outside development.");
        }

        if (authRequired && string.IsNullOrWhiteSpace(authority))
        {
            warnings.Add("Authentication is required but Auth:Authority is not configured.");
        }

        if (authRequired && string.IsNullOrWhiteSpace(audience))
        {
            warnings.Add("Authentication is required but Auth:Audience is not configured.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(authMode, "disabled", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Auth mode should not be disabled outside development.");
        }

        if (!environment.IsDevelopment() && !tenantEnforced)
        {
            warnings.Add("Tenant enforcement should be explicitly reviewed before cloud promotion.");
        }

        return warnings;
    }

    private static string Read(
        IConfiguration configuration,
        string key,
        string fallback)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool ReadBool(
        IConfiguration configuration,
        string key,
        bool fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }

    private static string? FirstNonEmptyOrNull(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
