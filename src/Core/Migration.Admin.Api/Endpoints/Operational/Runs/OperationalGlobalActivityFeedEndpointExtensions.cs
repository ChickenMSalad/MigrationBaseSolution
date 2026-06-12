using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalActivityFeedEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalActivityFeedEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/activity/recent",
                async (
                    int? limit,
                    IOperationalGlobalActivityFeedService activityFeedService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await activityFeedService.GetRecentActivityAsync(
                        limit ?? 50,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalRecentActivity")
            .WithTags("Operational Store")
            .WithSummary("Gets recent global operational activity across runs.")
            .Produces<OperationalGlobalActivityFeedResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


