using Migration.ControlPlane.Auth;

namespace Migration.Admin.Api.Endpoints;

public static class EndpointPolicyInventoryEndpointExtensions
{
    public static RouteGroupBuilder MapEndpointPolicyInventoryEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/auth/endpoint-policy-inventory", (
                IEndpointPolicyInventoryService service) =>
            Results.Ok(service.GetSnapshot()))
            .WithName("GetEndpointPolicyInventory")
            .WithTags("Cloud")
            .WithSummary("Gets advisory endpoint-to-policy inventory.");

        return api;
    }
}


