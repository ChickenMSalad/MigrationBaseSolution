using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureMetricsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureMetricsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/metrics",
                async (
                    int? sampleLimit,
                    IOperationalGlobalFailureMetricsService metricsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await metricsService.GetMetricsAsync(
                        sampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureMetrics")
            .WithTags("Operational Store")
            .WithSummary("Gets aggregate metrics for recent operational failures.")
            .Produces<OperationalGlobalFailureMetricsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


