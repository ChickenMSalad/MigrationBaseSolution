using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalQueueDepthAnalyticsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalQueueDepthAnalyticsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue/depth",
                async (
                    IOperationalGlobalQueueDepthAnalyticsService queueDepthService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await queueDepthService.GetAnalyticsAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalQueueDepthAnalytics")
            .WithTags("Operational Store")
            .WithSummary("Gets global operational queue depth analytics.")
            .Produces<OperationalGlobalQueueDepthAnalyticsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


