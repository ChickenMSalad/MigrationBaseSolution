using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunTimelineMetricsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunTimelineMetricsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/{runId:guid}/timeline/metrics",
                async (
                    Guid runId,
                    IOperationalRunTimelineMetricsService metricsService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await metricsService.GetMetricsAsync(
                            runId,
                            cancellationToken);

                        return response is null
                            ? Results.NotFound(new AdminApiErrorResponse("Operational run was not found."))
                            : Results.Ok(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("GetOperationalRunTimelineMetrics")
            .WithTags("Operational Store")
            .WithSummary("Gets operational run timeline metrics.")
            .Produces<OperationalRunTimelineMetricsResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}
