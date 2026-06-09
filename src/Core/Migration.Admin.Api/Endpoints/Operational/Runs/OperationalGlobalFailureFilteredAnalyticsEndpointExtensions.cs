using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureFilteredAnalyticsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureFilteredAnalyticsEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/filtered-analytics",
                async (
                    Guid? runId,
                    string? failureType,
                    bool? isRetriable,
                    string? sourceSystem,
                    string? targetSystem,
                    string? q,
                    int? limit,
                    IOperationalGlobalFailureFilteredAnalyticsService analyticsService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await analyticsService.GetAnalyticsAsync(
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
            .WithName("GetOperationalGlobalFailureFilteredAnalytics")
            .WithTags("Operational Store")
            .WithSummary("Gets filtered operational failure results and matching metrics.")
            .Produces<OperationalGlobalFailureFilteredAnalyticsResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


