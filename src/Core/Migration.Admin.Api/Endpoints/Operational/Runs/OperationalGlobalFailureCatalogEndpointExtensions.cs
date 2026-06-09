using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureCatalogEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureCatalogEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/catalog",
                async (
                    int? sampleLimit,
                    IOperationalGlobalFailureCatalogService catalogService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await catalogService.GetCatalogAsync(
                        sampleLimit ?? 500,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureCatalog")
            .WithTags("Operational Store")
            .WithSummary("Gets catalog values for global operational failure filters.")
            .Produces<OperationalGlobalFailureCatalogResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


