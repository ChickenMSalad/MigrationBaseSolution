using Migration.ControlPlane.Telemetry;

namespace Migration.Admin.Api.Endpoints;

public static class CloudOperationTelemetryEndpointExtensions
{
    public static RouteGroupBuilder MapCloudOperationTelemetryEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/telemetry/operation/event-names", () =>
            Results.Ok(new
            {
                category = TelemetryCategories.Cloud,
                eventNames = new[]
                {
                    CloudOperationTelemetryEventNames.ReadinessChecked,
                    CloudOperationTelemetryEventNames.StorageProviderChecked,
                    CloudOperationTelemetryEventNames.CredentialProviderChecked,
                    CloudOperationTelemetryEventNames.ArtifactStorageChecked,
                    CloudOperationTelemetryEventNames.DeploymentProfileChecked
                }
            }))
            .WithName("GetCloudOperationTelemetryEventNames")
            .WithTags("Cloud")
            .WithSummary("Gets cloud operation telemetry event names.");

        api.MapPost("/cloud/telemetry/operation/probe", async (
                ITelemetryEventWriter telemetryWriter,
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
                    CloudOperationTelemetryEventFactory.ReadinessChecked(
                        workspaceId,
                        isCloudReady: false,
                        warningCount: 1),
                    CloudOperationTelemetryEventFactory.ProviderChecked(
                        workspaceId,
                        CloudOperationTelemetryEventNames.StorageProviderChecked,
                        providerKind: "localFileSystem",
                        isConfigured: true,
                        warningCount: 0),
                    CloudOperationTelemetryEventFactory.ProviderChecked(
                        workspaceId,
                        CloudOperationTelemetryEventNames.CredentialProviderChecked,
                        providerKind: "userSecrets",
                        isConfigured: true,
                        warningCount: 0)
                };

                var results = new List<TelemetryWriteResult>();

                foreach (var telemetryEvent in events)
                {
                    results.Add(await telemetryWriter.WriteAsync(telemetryEvent, cancellationToken).ConfigureAwait(false));
                }

                return Results.Ok(new
                {
                    workspaceId,
                    eventCount = events.Length,
                    results
                });
            })
            .WithName("ProbeCloudOperationTelemetry")
            .WithTags("Cloud")
            .WithSummary("Writes synthetic cloud operation telemetry events.");

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


