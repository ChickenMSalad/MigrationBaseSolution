using Migration.ControlPlane.Audit;

namespace Migration.Admin.Api.Endpoints;

public static class CloudOperationAuditEndpointExtensions
{
    public static RouteGroupBuilder MapCloudOperationAuditEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/audit/operation/event-names", () =>
            Results.Ok(new
            {
                category = AuditCategories.Cloud,
                eventNames = new[]
                {
                    CloudOperationAuditEventNames.ReadinessChecked,
                    CloudOperationAuditEventNames.StorageProviderChecked,
                    CloudOperationAuditEventNames.CredentialProviderChecked,
                    CloudOperationAuditEventNames.ArtifactStorageChecked,
                    CloudOperationAuditEventNames.DeploymentProfileChecked
                }
            }))
            .WithName("GetCloudOperationAuditEventNames")
            .WithTags("Cloud")
            .WithSummary("Gets cloud operation audit event names.");

        api.MapPost("/cloud/audit/operation/probe", async (
                IAuditEventWriter auditWriter,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var events = new[]
                {
                    CloudOperationAuditEventFactory.ReadinessChecked(
                        workspaceId,
                        isCloudReady: false,
                        warningCount: 1),
                    CloudOperationAuditEventFactory.ProviderChecked(
                        workspaceId,
                        CloudOperationAuditEventNames.StorageProviderChecked,
                        providerKind: "localFileSystem",
                        isConfigured: true,
                        warningCount: 0),
                    CloudOperationAuditEventFactory.ProviderChecked(
                        workspaceId,
                        CloudOperationAuditEventNames.CredentialProviderChecked,
                        providerKind: "userSecrets",
                        isConfigured: true,
                        warningCount: 0)
                };

                var results = new List<AuditWriteResult>();

                foreach (var auditEvent in events)
                {
                    results.Add(await auditWriter.WriteAsync(auditEvent, cancellationToken).ConfigureAwait(false));
                }

                return Results.Ok(new
                {
                    workspaceId,
                    eventCount = events.Length,
                    results
                });
            })
            .WithName("ProbeCloudOperationAudit")
            .WithTags("Cloud")
            .WithSummary("Writes synthetic cloud operation audit events.");

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


