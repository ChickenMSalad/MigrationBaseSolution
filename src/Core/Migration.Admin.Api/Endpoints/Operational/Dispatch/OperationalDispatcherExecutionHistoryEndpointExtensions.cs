using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalDispatcherExecutionHistoryEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalDispatcherExecutionHistoryEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/dispatcher/executions",
                async (
                    IDispatcherExecutionHistoryService historyService,
                    CancellationToken cancellationToken) =>
                {
                    var results = await historyService.GetRecentAsync(
                        100,
                        cancellationToken);

                    return Results.Ok(results);
                })
            .WithName("GetOperationalDispatcherExecutions")
            .WithTags("Operational Store")
            .WithSummary("Gets recent operational dispatcher executions.")
            .Produces<IReadOnlyCollection<DispatcherExecutionRecord>>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapGet(
                "/operational/dispatcher/executions/{executionId:guid}",
                async (
                    Guid executionId,
                    IDispatcherExecutionHistoryService historyService,
                    CancellationToken cancellationToken) =>
                {
                    var result = await historyService.GetAsync(
                        executionId,
                        cancellationToken);

                    return result is null
                        ? Results.NotFound()
                        : Results.Ok(result);
                })
            .WithName("GetOperationalDispatcherExecution")
            .WithTags("Operational Store")
            .WithSummary("Gets one operational dispatcher execution.")
            .Produces<DispatcherExecutionRecord>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}


