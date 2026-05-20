using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalRunHealthSummaryEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalRunHealthSummaryEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/health-summary",
                async (
                    IOperationalGlobalRunHealthSummaryService healthService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await healthService.GetSummaryAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalRunHealthSummary")
            .WithTags("Operational Store")
            .WithSummary("Gets a global operational run health summary.")
            .Produces<OperationalGlobalRunHealthSummaryResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
