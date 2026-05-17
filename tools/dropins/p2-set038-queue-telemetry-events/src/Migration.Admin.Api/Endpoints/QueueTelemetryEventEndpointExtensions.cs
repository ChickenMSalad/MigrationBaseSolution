using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Telemetry;

namespace Migration.Admin.Api.Endpoints;

public static class QueueTelemetryEventEndpointExtensions
{
    public static RouteGroupBuilder MapQueueTelemetryEventEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/telemetry/event-names", () =>
            Results.Ok(new
            {
                category = TelemetryCategories.Queue,
                eventNames = new[]
                {
                    QueueTelemetryEventNames.DispatchAccepted,
                    QueueTelemetryEventNames.ReceivePolled,
                    QueueTelemetryEventNames.MessagePlanned,
                    QueueTelemetryEventNames.MessageFailed,
                    QueueTelemetryEventNames.CoordinatorPolled
                }
            }))
            .WithName("GetQueueTelemetryEventNames")
            .WithTags("Cloud")
            .WithSummary("Gets queue telemetry event names.");

        api.MapPost("/cloud/queue/telemetry/probe", async (
                ITelemetryEventWriter telemetryWriter,
                IQueueExecutionPlanner planner,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var envelope = QueueMessageEnvelopeFactory.CreateMigrationRunEnvelope(
                    workspaceId,
                    "sample-project",
                    "sample-run",
                    QueueMessageTypes.MigrationRunExecute);

                var plan = planner.Plan(envelope);

                var events = new[]
                {
                    QueueTelemetryEventFactory.DispatchAccepted(envelope, "diagnostic", "migration-runs"),
                    QueueTelemetryEventFactory.MessagePlanned(envelope, plan)
                };

                var results = new List<TelemetryWriteResult>();

                foreach (var telemetryEvent in events)
                {
                    results.Add(await telemetryWriter.WriteAsync(telemetryEvent, cancellationToken).ConfigureAwait(false));
                }

                return Results.Ok(new
                {
                    envelope,
                    plan,
                    telemetryResults = results
                });
            })
            .WithName("ProbeQueueTelemetryEvents")
            .WithTags("Cloud")
            .WithSummary("Writes synthetic queue telemetry events.");

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
