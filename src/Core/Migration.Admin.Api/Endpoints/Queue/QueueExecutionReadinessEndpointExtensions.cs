using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueExecutionReadinessEndpointExtensions
{
    public static RouteGroupBuilder MapQueueExecutionReadinessEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/execution-readiness", (
                IQueueExecutionReadinessService service) =>
            Results.Ok(service.GetSnapshot()))
            .WithName("GetQueueExecutionReadiness")
            .WithTags("Cloud")
            .WithSummary("Gets queue execution readiness rollup.");

        return api;
    }
}


