using Migration.ControlPlane.Operations;

namespace Migration.Admin.Api.Endpoints;

public static class ProductionSafetyGateEndpointExtensions
{
    public static RouteGroupBuilder MapProductionSafetyGateEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/operations/production-safety-gates", (
                IProductionSafetyGateService service) =>
            Results.Ok(service.GetSnapshot()))
            .WithName("GetProductionSafetyGates")
            .WithTags("Cloud")
            .WithSummary("Gets production safety gate rollup.");

        return api;
    }
}
