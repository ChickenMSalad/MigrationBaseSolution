using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueExecutionObservabilityEndpointExtensions
{
    public static RouteGroupBuilder MapQueueExecutionObservabilityEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/execution-observability", (
                IQueueExecutionObservabilityService service) =>
            {
                var snapshot = service.GetSnapshot();
                return Results.Ok(snapshot);
            })
            .WithName("GetQueueExecutionObservability")
            .WithTags("Cloud")
            .WithSummary("Gets queue execution observability snapshot.");

        return api;
    }
}


