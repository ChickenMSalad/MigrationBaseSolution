using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalDispatcherExecutionHistoryRetentionEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalDispatcherExecutionHistoryRetentionEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/dispatcher/executions/retention/status",
                async (
                    IDispatcherExecutionHistoryRetentionService retentionService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await retentionService.GetStatusAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalDispatcherExecutionHistoryRetentionStatus")
            .WithTags("Operational Store")
            .WithSummary("Gets dispatcher execution history retention status.")
            .Produces<DispatcherExecutionHistoryRetentionStatusResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapPost(
                "/operational/dispatcher/executions/retention/purge-eligible",
                async (
                    IDispatcherExecutionHistoryRetentionService retentionService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await retentionService.PurgeEligibleAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("PurgeEligibleOperationalDispatcherExecutionHistory")
            .WithTags("Operational Store")
            .WithSummary("Purges eligible dispatcher execution history when retention is enabled.")
            .Produces<DispatcherExecutionHistoryRetentionPurgeResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


