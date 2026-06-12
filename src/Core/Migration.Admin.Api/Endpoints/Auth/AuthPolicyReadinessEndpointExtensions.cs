using Migration.ControlPlane.Auth;

namespace Migration.Admin.Api.Endpoints;

public static class AuthPolicyReadinessEndpointExtensions
{
    public static RouteGroupBuilder MapAuthPolicyReadinessEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/auth/policy-readiness", (
                IAuthPolicyReadinessService service) =>
            Results.Ok(service.GetSnapshot()))
            .WithName("GetAuthPolicyReadiness")
            .WithTags("Cloud")
            .WithSummary("Gets auth policy readiness diagnostics.");

        return api;
    }
}


