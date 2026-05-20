using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalRunHealthDashboardEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalRunHealthDashboardEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/health-dashboard",
                async (
                    int? activityLimit,
                    int? failureLimit,
                    int? metricsSampleLimit,
                    IOperationalGlobalRunHealthDashboardService dashboardService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await dashboardService.GetDashboardAsync(
                        activityLimit ?? 10,
                        failureLimit ?? 10,
                        metricsSampleLimit ?? 100,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalRunHealthDashboard")
            .WithTags("Operational Store")
            .WithSummary("Gets a global operational run health dashboard.")
            .Produces<OperationalGlobalRunHealthDashboardResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
