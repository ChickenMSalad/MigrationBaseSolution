using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/recent",
                async (
                    int? limit,
                    IOperationalGlobalFailureService failureService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await failureService.GetRecentFailuresAsync(
                        limit ?? 50,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalRecentFailures")
            .WithTags("Operational Store")
            .WithSummary("Gets recent operational failures across runs.")
            .Produces<OperationalGlobalRecentFailuresResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


