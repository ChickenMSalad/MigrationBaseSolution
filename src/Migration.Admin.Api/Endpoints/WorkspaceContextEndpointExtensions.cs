using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class WorkspaceContextEndpointExtensions
{
    public static RouteGroupBuilder MapWorkspaceContextEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/workspace/context", (
                IConfiguration configuration,
                IWebHostEnvironment environment,
                HttpContext httpContext) =>
            {
                var descriptor = BuildDescriptor(configuration, environment, httpContext);
                return Results.Ok(descriptor);
            })
            .WithName("GetWorkspaceContext")
            .WithTags("Workspace")
            .WithSummary("Gets the current workspace/tenant context shape for cloud-readiness diagnostics.")
            .Produces<WorkspaceContextDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static WorkspaceContextDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        HttpContext httpContext)
    {
        var tenantMode = Read(
            configuration,
            "Workspace:TenantMode",
            environment.IsDevelopment()
                ? WorkspaceTenantModes.Development
                : WorkspaceTenantModes.SingleTenant);

        var tenantId = FirstNonEmpty(
            httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault(),
            configuration["Workspace:TenantId"]);

        var workspaceId = FirstNonEmpty(
            httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
            configuration["Workspace:WorkspaceId"],
            "default");

        var displayName = Read(
            configuration,
            "Workspace:DisplayName",
            workspaceId == "default" ? "Default Workspace" : workspaceId);

        var tenantEnforced = ReadBool(
            configuration,
            "Workspace:TenantEnforced",
            !environment.IsDevelopment() &&
            string.Equals(tenantMode, WorkspaceTenantModes.MultiTenant, StringComparison.OrdinalIgnoreCase));

        var warnings = BuildWarnings(environment, tenantMode, tenantEnforced, tenantId, workspaceId);

        return new WorkspaceContextDescriptor(
            WorkspaceId: workspaceId,
            DisplayName: displayName,
            TenantMode: tenantMode,
            IsDefaultWorkspace: string.Equals(workspaceId, "default", StringComparison.OrdinalIgnoreCase),
            IsTenantEnforced: tenantEnforced,
            TenantId: string.IsNullOrWhiteSpace(tenantId) ? null : tenantId,
            AllowedConnectorRoles: new[] { "source", "target", "manifestProvider" },
            Warnings: warnings);
    }

    private static List<string> BuildWarnings(
        IWebHostEnvironment environment,
        string tenantMode,
        bool tenantEnforced,
        string? tenantId,
        string workspaceId)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() &&
            string.Equals(workspaceId, "default", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should use an explicit workspace id.");
        }

        if (string.Equals(tenantMode, WorkspaceTenantModes.MultiTenant, StringComparison.OrdinalIgnoreCase) &&
            !tenantEnforced)
        {
            warnings.Add("Tenant mode is multiTenant but tenant enforcement is disabled.");
        }

        if (tenantEnforced && string.IsNullOrWhiteSpace(tenantId))
        {
            warnings.Add("Tenant enforcement is enabled but no tenant id was supplied.");
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
