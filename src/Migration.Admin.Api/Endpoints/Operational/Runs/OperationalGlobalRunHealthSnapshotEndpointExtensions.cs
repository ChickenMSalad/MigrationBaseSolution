using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalRunHealthSnapshotEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalRunHealthSnapshotEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/health-snapshot",
                async (
                    int? recentLimit,
                    int? metricsSampleLimit,
                    IOperationalGlobalRunHealthSnapshotService snapshotService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await snapshotService.GetSnapshotAsync(
                        recentLimit ?? 25,
                        metricsSampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalRunHealthSnapshot")
            .WithTags("Operational Store")
            .WithSummary("Gets a point-in-time global operational run health snapshot.")
            .Produces<OperationalGlobalRunHealthSnapshotResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
