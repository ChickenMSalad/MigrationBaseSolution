using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalDispatcherDashboardEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalDispatcherDashboardEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/dispatcher/dashboard",
                async (
                    IOperationalDispatcherDashboardSummaryService dashboardService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await dashboardService.GetSummaryAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalDispatcherDashboard")
            .WithTags("Operational Store")
            .WithSummary("Gets a consolidated operational dispatcher dashboard summary.")
            .Produces<OperationalDispatcherDashboardSummaryResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
