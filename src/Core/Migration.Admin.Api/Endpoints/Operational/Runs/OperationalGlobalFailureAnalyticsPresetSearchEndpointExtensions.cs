using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureAnalyticsPresetSearchEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureAnalyticsPresetSearchEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/analytics-presets/search",
                async (
                    string? q,
                    int? limit,
                    IOperationalGlobalFailureAnalyticsPresetSearchService searchService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await searchService.SearchAsync(
                        q,
                        limit ?? 50,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("SearchOperationalGlobalFailureAnalyticsPresets")
            .WithTags("Operational Store")
            .WithSummary("Searches operational failure analytics presets.")
            .Produces<OperationalGlobalFailureAnalyticsPresetSearchResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return api;
    }
}


