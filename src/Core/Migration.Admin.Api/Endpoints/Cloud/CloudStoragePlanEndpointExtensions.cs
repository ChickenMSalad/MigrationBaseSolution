using Migration.ControlPlane.Storage;

namespace Migration.Admin.Api.Endpoints;

public static class CloudStoragePlanEndpointExtensions
{
    public static RouteGroupBuilder MapCloudStoragePlanEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/storage/locations", (
                ICloudStoragePathResolver resolver,
                HttpContext httpContext,
                IConfiguration configuration) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var projectId = FirstNonEmpty(
                    httpContext.Request.Query["projectId"].FirstOrDefault(),
                    "sample-project");

                var runId = FirstNonEmpty(
                    httpContext.Request.Query["runId"].FirstOrDefault(),
                    "sample-run");

                return Results.Ok(new
                {
                    workspaceId,
                    workspaceRoot = resolver.ResolveWorkspaceRoot(workspaceId),
                    projectRoot = resolver.ResolveProjectRoot(workspaceId, projectId),
                    runRoot = resolver.ResolveRunRoot(workspaceId, runId),
                    manifestArtifactsRoot = resolver.ResolveArtifactRoot(workspaceId, "manifest"),
                    mappingArtifactsRoot = resolver.ResolveArtifactRoot(workspaceId, "mapping"),
                    taxonomyArtifactsRoot = resolver.ResolveArtifactRoot(workspaceId, "taxonomy"),
                    auditRoot = resolver.ResolveAuditRoot(workspaceId)
                });
            })
            .WithName("GetCloudStorageLocations")
            .WithTags("Cloud")
            .WithSummary("Gets resolved logical cloud storage locations for workspace-scoped resources.");

        return api;
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
