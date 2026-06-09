using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunTimelineQueryEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunTimelineQueryEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/{runId:guid}/timeline/query",
                async (
                    Guid runId,
                    string? eventType,
                    string? source,
                    int? limit,
                    IOperationalRunTimelineQueryService timelineQueryService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await timelineQueryService.QueryTimelineAsync(
                            runId,
                            new OperationalRunTimelineQuery
                            {
                                EventType = eventType,
                                Source = source,
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
            .WithName("QueryOperationalRunTimeline")
            .WithTags("Operational Store")
            .WithSummary("Queries a filtered operational run timeline.")
            .Produces<OperationalRunTimelineResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}


