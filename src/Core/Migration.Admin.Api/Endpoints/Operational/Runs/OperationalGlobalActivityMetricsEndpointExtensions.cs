using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalActivityMetricsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalActivityMetricsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/activity/metrics",
                async (
                    int? sampleLimit,
                    IOperationalGlobalActivityMetricsService metricsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await metricsService.GetMetricsAsync(
                        sampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalActivityMetrics")
            .WithTags("Operational Store")
            .WithSummary("Gets aggregate metrics for recent global operational activity.")
            .Produces<OperationalGlobalActivityMetricsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


