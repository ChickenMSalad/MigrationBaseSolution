using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureAnalyticsPresetDashboardEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureAnalyticsPresetDashboardEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/analytics-preset-dashboard",
                async (
                    string? presetKey,
                    int? limit,
                    IOperationalGlobalFailureAnalyticsPresetDashboardService dashboardService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await dashboardService.GetDashboardAsync(
                        presetKey ?? "all-recent",
                        limit ?? 50,
                        cancellationToken);

                    return response is null
                        ? Results.NotFound(new AdminApiErrorResponse("Operational failure analytics preset was not found."))
                        : Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureAnalyticsPresetDashboard")
            .WithTags("Operational Store")
            .WithSummary("Gets the operational failure analytics preset dashboard.")
            .Produces<OperationalGlobalFailureAnalyticsPresetDashboardResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}

