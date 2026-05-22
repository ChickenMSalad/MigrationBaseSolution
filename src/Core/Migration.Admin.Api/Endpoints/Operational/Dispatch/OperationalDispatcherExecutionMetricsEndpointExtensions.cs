using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalDispatcherExecutionMetricsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalDispatcherExecutionMetricsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/dispatcher/executions/metrics",
                async (
                    IDispatcherExecutionHistoryMetricsService metricsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await metricsService.GetMetricsAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalDispatcherExecutionMetrics")
            .WithTags("Operational Store")
            .WithSummary("Gets dispatcher execution history metrics.")
            .Produces<DispatcherExecutionHistoryMetricsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
