using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunStatusProjectionEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunStatusProjectionEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/status-projections",
                async (
                    IOperationalRunStatusProjectionService projectionService,
                    CancellationToken cancellationToken) =>
                {
                    var projections = await projectionService.ListAsync(
                        cancellationToken);

                    return Results.Ok(projections);
                })
            .WithName("GetOperationalRunStatusProjections")
            .WithTags("Operational Store")
            .WithSummary("Lists operational run status projections.")
            .Produces<IReadOnlyCollection<OperationalRunStatusProjection>>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapGet(
                "/operational/runs/{runId:guid}/status-projection",
                async (
                    Guid runId,
                    IOperationalRunStatusProjectionService projectionService,
                    CancellationToken cancellationToken) =>
                {
                    var projection = await projectionService.GetAsync(
                        runId,
                        cancellationToken);

                    return projection is null
                        ? Results.NotFound()
                        : Results.Ok(projection);
                })
            .WithName("GetOperationalRunStatusProjection")
            .WithTags("Operational Store")
            .WithSummary("Gets operational run status projection detail.")
            .Produces<OperationalRunStatusProjection>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}
