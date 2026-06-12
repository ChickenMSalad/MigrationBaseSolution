using Migration.ControlPlane.Auth;

namespace Migration.Admin.Api.Endpoints;

public static class CredentialAccessPolicyReadinessEndpointExtensions
{
    public static RouteGroupBuilder MapCredentialAccessPolicyReadinessEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/auth/credential-access-policy", (
                ICredentialAccessPolicyReadinessService service) =>
            Results.Ok(service.GetSnapshot()))
            .WithName("GetCredentialAccessPolicyReadiness")
            .WithTags("Cloud")
            .WithSummary("Gets credential access policy readiness diagnostics.");

        return api;
    }
}


