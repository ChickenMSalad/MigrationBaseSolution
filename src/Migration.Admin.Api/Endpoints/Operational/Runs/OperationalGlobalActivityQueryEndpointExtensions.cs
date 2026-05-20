using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalActivityQueryEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalActivityQueryEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/activity/query",
                async (
                    Guid? runId,
                    string? eventType,
                    string? source,
                    string? q,
                    int? limit,
                    IOperationalGlobalActivityQueryService activityQueryService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await activityQueryService.QueryRecentActivityAsync(
                        new OperationalGlobalActivityQuery
                        {
                            RunId = runId,
                            EventType = eventType,
                            Source = source,
                            SearchText = q,
                            Limit = limit ?? 50
                        },
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("QueryOperationalRecentActivity")
            .WithTags("Operational Store")
            .WithSummary("Queries recent global operational activity across runs.")
            .Produces<OperationalGlobalActivityFeedResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
