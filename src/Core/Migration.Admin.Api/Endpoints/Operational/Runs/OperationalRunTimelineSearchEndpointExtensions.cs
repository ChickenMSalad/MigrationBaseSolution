using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunTimelineSearchEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunTimelineSearchEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/{runId:guid}/timeline/search",
                async (
                    Guid runId,
                    string? q,
                    int? limit,
                    IOperationalRunTimelineSearchService searchService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await searchService.SearchAsync(
                            runId,
                            new OperationalRunTimelineSearchQuery
                            {
                                SearchText = q ?? string.Empty,
                                Limit = limit ?? 100
                            },
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
            .WithName("SearchOperationalRunTimeline")
            .WithTags("Operational Store")
            .WithSummary("Searches operational run timeline events.")
            .Produces<OperationalRunTimelineResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}


