using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalDispatcherExecutionHistoryQueryEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalDispatcherExecutionHistoryQueryEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/dispatcher/executions/query",
                async (
                    string? workerId,
                    string? outcome,
                    int? limit,
                    IDispatcherExecutionHistoryQueryService queryService,
                    CancellationToken cancellationToken) =>
                {
                    var query = new DispatcherExecutionHistoryQuery
                    {
                        WorkerId = workerId,
                        Outcome = outcome,
                        Limit = limit ?? 50
                    };

                    var response = await queryService.QueryAsync(
                        query,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("QueryOperationalDispatcherExecutions")
            .WithTags("Operational Store")
            .WithSummary("Queries dispatcher execution history.")
            .Produces<IReadOnlyCollection<DispatcherExecutionRecord>>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


