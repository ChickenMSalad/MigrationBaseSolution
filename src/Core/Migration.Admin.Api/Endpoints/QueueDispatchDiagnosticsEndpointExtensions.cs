using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueDispatchDiagnosticsEndpointExtensions
{
    public static RouteGroupBuilder MapQueueDispatchDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/dispatch/provider", (
                IQueueDispatchProvider provider) =>
            Results.Ok(provider.Descriptor))
            .WithName("GetQueueDispatchProvider")
            .WithTags("Cloud")
            .WithSummary("Gets active queue dispatch provider diagnostics.");

        api.MapPost("/cloud/queue/dispatch/probe", async (
                IQueueDispatchProvider provider,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
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
                    Guid.NewGuid().ToString("N"));

                var envelope = QueueMessageEnvelopeFactory.CreateMigrationRunEnvelope(
                    workspaceId,
                    projectId,
                    runId,
                    "migration.run.execute");

                var result = await provider.DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    envelope,
                    result
                });
            })
            .WithName("ProbeQueueDispatch")
            .WithTags("Cloud")
            .WithSummary("Dispatches a sample queue envelope through the active dispatch provider.");

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


