using Migration.ControlPlane.Operations;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalModeEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalModeEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/operations/mode", (
                IOperationalModeService service) =>
            Results.Ok(service.GetSnapshot()))
            .WithName("GetOperationalMode")
            .WithTags("Cloud")
            .WithSummary("Gets current operational mode/state.");

        return api;
    }
}
