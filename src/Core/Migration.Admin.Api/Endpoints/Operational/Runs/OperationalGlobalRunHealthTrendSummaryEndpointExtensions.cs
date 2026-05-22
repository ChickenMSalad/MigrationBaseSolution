using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalRunHealthTrendSummaryEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalRunHealthTrendSummaryEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/health-trend-summary",
                async (
                    int? recentLimit,
                    int? metricsSampleLimit,
                    IOperationalGlobalRunHealthTrendSummaryService trendService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await trendService.GetTrendSummaryAsync(
                        recentLimit ?? 25,
                        metricsSampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalRunHealthTrendSummary")
            .WithTags("Operational Store")
            .WithSummary("Gets a trend-style summary for global operational run health.")
            .Produces<OperationalGlobalRunHealthTrendSummaryResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
