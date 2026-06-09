using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureSystemPairMetricsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureSystemPairMetricsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/system-pair-metrics",
                async (
                    int? sampleLimit,
                    IOperationalGlobalFailureSystemPairMetricsService metricsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await metricsService.GetMetricsAsync(
                        sampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureSystemPairMetrics")
            .WithTags("Operational Store")
            .WithSummary("Gets operational failure metrics grouped by source/target system pair.")
            .Produces<OperationalGlobalFailureSystemPairMetricsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}

