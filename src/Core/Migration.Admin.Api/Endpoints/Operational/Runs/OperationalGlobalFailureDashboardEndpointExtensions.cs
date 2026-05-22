using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureDashboardEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureDashboardEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/dashboard",
                async (
                    int? recentLimit,
                    int? metricsSampleLimit,
                    IOperationalGlobalFailureDashboardService dashboardService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await dashboardService.GetDashboardAsync(
                        recentLimit ?? 25,
                        metricsSampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureDashboard")
            .WithTags("Operational Store")
            .WithSummary("Gets a consolidated global operational failure dashboard.")
            .Produces<OperationalGlobalFailureDashboardResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
