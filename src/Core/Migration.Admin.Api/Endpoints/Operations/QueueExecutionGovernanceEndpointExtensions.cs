using Migration.ControlPlane.Operations;

namespace Migration.Admin.Api.Endpoints;

public static class QueueExecutionGovernanceEndpointExtensions
{
    public static RouteGroupBuilder MapQueueExecutionGovernanceEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/operations/queue-execution-governance", (
                IQueueExecutionGovernanceService service) =>
            Results.Ok(service.GetDecision()))
            .WithName("GetQueueExecutionGovernance")
            .WithTags("Cloud")
            .WithSummary("Gets queue execution governance decision.");

        return api;
    }
}


