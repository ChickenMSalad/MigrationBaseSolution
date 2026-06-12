using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalRunHealthOperationsCenterEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalRunHealthOperationsCenterEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/health-operations-center",
                async (
                    int? activityRecentLimit,
                    int? metricsSampleLimit,
                    IOperationalGlobalRunHealthOperationsCenterService operationsCenterService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await operationsCenterService.GetOperationsCenterAsync(
                        activityRecentLimit ?? 25,
                        metricsSampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalRunHealthOperationsCenter")
            .WithTags("Operational Store")
            .WithSummary("Gets a consolidated operational run health operations center payload.")
            .Produces<OperationalGlobalRunHealthOperationsCenterResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


