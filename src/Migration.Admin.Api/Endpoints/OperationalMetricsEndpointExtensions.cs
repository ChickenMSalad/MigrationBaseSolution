using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalMetricsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalMetricsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/metrics/work-items",
                async (
                    IOperationalMetricsService metricsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await metricsService.GetWorkItemMetricsAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalWorkItemMetrics")
            .WithTags("Operational Store")
            .WithSummary("Returns SQL-derived operational work-item metrics.")
            .Produces<OperationalWorkItemMetricsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapGet(
                "/operational/metrics/leases",
                async (
                    IOperationalMetricsService metricsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await metricsService.GetLeaseMetricsAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalLeaseMetrics")
            .WithTags("Operational Store")
            .WithSummary("Returns SQL-derived operational lease metrics.")
            .Produces<OperationalLeaseMetricsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapGet(
                "/operational/metrics/runs",
                async (
                    IOperationalMetricsService metricsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await metricsService.GetRunMetricsAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalRunMetrics")
            .WithTags("Operational Store")
            .WithSummary("Returns SQL-derived operational run metrics.")
            .Produces<OperationalRunMetricsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapGet(
                "/operational/diagnostics/summary",
                async (
                    IOperationalMetricsService metricsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await metricsService.GetSummaryAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalDiagnosticsSummary")
            .WithTags("Operational Store")
            .WithSummary("Returns SQL-derived operational diagnostics summary.")
            .Produces<OperationalDiagnosticsSummaryResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
