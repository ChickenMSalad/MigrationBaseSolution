using Migration.ControlPlane.Operations;

namespace Migration.Admin.Api.Endpoints;

public static class P2ReadinessReportEndpointExtensions
{
    public static RouteGroupBuilder MapP2ReadinessReportEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/operations/p2-readiness-report", (
                IP2ReadinessReportService service) =>
            Results.Ok(service.GetSnapshot()))
            .WithName("GetP2ReadinessReport")
            .WithTags("Cloud")
            .WithSummary("Gets consolidated P2 readiness report.");

        return api;
    }
}
