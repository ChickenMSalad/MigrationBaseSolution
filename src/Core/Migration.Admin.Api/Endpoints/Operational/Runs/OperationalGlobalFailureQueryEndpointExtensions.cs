using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureQueryEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureQueryEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/query",
                async (
                    Guid? runId,
                    string? failureType,
                    bool? isRetriable,
                    string? sourceSystem,
                    string? targetSystem,
                    string? q,
                    int? limit,
                    IOperationalGlobalFailureQueryService queryService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await queryService.QueryRecentFailuresAsync(
                        new OperationalGlobalFailureQuery
                        {
                            RunId = runId,
                            FailureType = failureType,
                            IsRetriable = isRetriable,
                            SourceSystem = sourceSystem,
                            TargetSystem = targetSystem,
                            SearchText = q,
                            Limit = limit ?? 50
                        },
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("QueryOperationalGlobalFailures")
            .WithTags("Operational Store")
            .WithSummary("Queries recent operational failures across runs.")
            .Produces<OperationalGlobalRecentFailuresResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}
