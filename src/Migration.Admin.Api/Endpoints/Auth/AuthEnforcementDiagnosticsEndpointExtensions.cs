using Migration.ControlPlane.Auth;

namespace Migration.Admin.Api.Endpoints;

public static class AuthEnforcementDiagnosticsEndpointExtensions
{
    public static RouteGroupBuilder MapAuthEnforcementDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/auth/enforcement-diagnostics", (
                IAuthEnforcementDiagnosticsService service) =>
            Results.Ok(service.GetSnapshot()))
            .WithName("GetAuthEnforcementDiagnostics")
            .WithTags("Cloud")
            .WithSummary("Gets auth enforcement diagnostics.");

        return api;
    }
}
