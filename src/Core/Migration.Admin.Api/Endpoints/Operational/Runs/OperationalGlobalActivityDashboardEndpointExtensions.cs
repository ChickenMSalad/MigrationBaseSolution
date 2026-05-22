using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalActivityDashboardEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalActivityDashboardEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/activity/dashboard",
                async (
                    int? recentLimit,
                    int? metricsSampleLimit,
                    IOperationalGlobalActivityDashboardService dashboardService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await dashboardService.GetDashboardAsync(
                        recentLimit ?? 25,
                        metricsSampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalActivityDashboard")
            .WithTags("Operational Store")
            .WithSummary("Gets a consolidated global operational activity dashboard.")
            .Produces<OperationalGlobalActivityDashboardResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
