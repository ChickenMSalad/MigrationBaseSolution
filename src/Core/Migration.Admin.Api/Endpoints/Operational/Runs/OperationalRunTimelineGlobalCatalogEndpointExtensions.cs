using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunTimelineGlobalCatalogEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunTimelineGlobalCatalogEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/timeline/catalog",
                async (
                    IOperationalRunTimelineGlobalCatalogService catalogService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await catalogService.GetCatalogAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalRunTimelineGlobalCatalog")
            .WithTags("Operational Store")
            .WithSummary("Gets global operational run timeline event type and source catalog values.")
            .Produces<OperationalRunTimelineGlobalCatalogResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
