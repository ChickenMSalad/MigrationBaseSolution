using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalDispatcherPressureAnalyticsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalDispatcherPressureAnalyticsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/dispatcher/pressure",
                async (
                    int? metricsSampleLimit,
                    IOperationalDispatcherPressureAnalyticsService pressureService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await pressureService.GetAnalyticsAsync(
                        metricsSampleLimit ?? 100,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalDispatcherPressureAnalytics")
            .WithTags("Operational Store")
            .WithSummary("Gets dispatcher pressure analytics using queue depth and dispatcher execution signals.")
            .Produces<OperationalDispatcherPressureAnalyticsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
