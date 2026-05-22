using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunTimelineCatalogEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunTimelineCatalogEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/{runId:guid}/timeline/catalog",
                async (
                    Guid runId,
                    IOperationalRunTimelineCatalogService catalogService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await catalogService.GetCatalogAsync(
                            runId,
                            cancellationToken);

                        return response is null
                            ? Results.NotFound(new AdminApiErrorResponse("Operational run was not found."))
                            : Results.Ok(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("GetOperationalRunTimelineCatalog")
            .WithTags("Operational Store")
            .WithSummary("Gets timeline event type and source catalog values for one operational run.")
            .Produces<OperationalRunTimelineCatalogResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}
