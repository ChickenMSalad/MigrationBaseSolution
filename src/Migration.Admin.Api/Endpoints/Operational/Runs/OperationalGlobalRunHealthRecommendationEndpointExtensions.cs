using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalRunHealthRecommendationEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalRunHealthRecommendationEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/health-recommendations",
                async (
                    int? recentLimit,
                    int? metricsSampleLimit,
                    IOperationalGlobalRunHealthRecommendationService recommendationService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await recommendationService.GetRecommendationsAsync(
                        recentLimit ?? 25,
                        metricsSampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalRunHealthRecommendations")
            .WithTags("Operational Store")
            .WithSummary("Gets prioritized recommendations for global operational run health.")
            .Produces<OperationalGlobalRunHealthRecommendationsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
