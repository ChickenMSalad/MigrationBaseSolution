using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureAnalyticsDashboardEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureAnalyticsDashboardEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/analytics-dashboard",
                async (
                    int? recentLimit,
                    int? metricsSampleLimit,
                    IOperationalGlobalFailureAnalyticsDashboardService dashboardService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await dashboardService.GetDashboardAsync(
                        recentLimit ?? 25,
                        metricsSampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureAnalyticsDashboard")
            .WithTags("Operational Store")
            .WithSummary("Gets a consolidated global operational failure analytics dashboard.")
            .Produces<OperationalGlobalFailureAnalyticsDashboardResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
