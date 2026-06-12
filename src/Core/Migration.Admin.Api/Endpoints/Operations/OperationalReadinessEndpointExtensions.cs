using Migration.ControlPlane.Operations;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalReadinessEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalReadinessEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/operations/readiness", (
                IOperationalReadinessService service) =>
            Results.Ok(service.GetSnapshot()))
            .WithName("GetOperationalReadiness")
            .WithTags("Cloud")
            .WithSummary("Gets operational readiness rollup across audit, telemetry, and queue execution.");

        return api;
    }
}


