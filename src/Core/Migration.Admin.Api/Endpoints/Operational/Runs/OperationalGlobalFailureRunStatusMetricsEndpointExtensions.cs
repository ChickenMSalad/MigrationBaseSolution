using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureRunStatusMetricsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureRunStatusMetricsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/run-status-metrics",
                async (
                    int? sampleLimit,
                    IOperationalGlobalFailureRunStatusMetricsService metricsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await metricsService.GetMetricsAsync(
                        sampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureRunStatusMetrics")
            .WithTags("Operational Store")
            .WithSummary("Gets operational failure metrics grouped by run status.")
            .Produces<OperationalGlobalFailureRunStatusMetricsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
