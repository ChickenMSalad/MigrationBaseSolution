using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalMirrorReadEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalMirrorReadEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs",
                async (
                    IOperationalMirrorReadService readService,
                    CancellationToken cancellationToken) =>
                {
                    var runs = await readService.ListRunsAsync(
                        cancellationToken);

                    return Results.Ok(runs);
                })
            .WithName("GetOperationalRuns")
            .WithTags("Operational Store")
            .WithSummary("Lists mirrored operational runs.")
            .Produces<IReadOnlyCollection<OperationalMirrorRunSummary>>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapGet(
                "/operational/runs/{runId:guid}",
                async (
                    Guid runId,
                    IOperationalMirrorReadService readService,
                    CancellationToken cancellationToken) =>
                {
                    var run = await readService.GetRunAsync(
                        runId,
                        cancellationToken);

                    return run is null
                        ? Results.NotFound()
                        : Results.Ok(run);
                })
            .WithName("GetOperationalRun")
            .WithTags("Operational Store")
            .WithSummary("Gets mirrored operational run detail.")
            .Produces<OperationalMirrorRunDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}
