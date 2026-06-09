using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureAnalyticsPresetFavoriteEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureAnalyticsPresetFavoriteEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/analytics-preset-favorites",
                async (
                    IOperationalGlobalFailureAnalyticsPresetFavoriteService favoriteService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await favoriteService.GetFavoritesAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureAnalyticsPresetFavorites")
            .WithTags("Operational Store")
            .WithSummary("Gets built-in favorite groups for operational failure analytics presets.")
            .Produces<OperationalGlobalFailureAnalyticsPresetFavoritesResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapGet(
                "/operational/failures/analytics-preset-favorites/{favoriteKey}",
                async (
                    string favoriteKey,
                    int? limit,
                    IOperationalGlobalFailureAnalyticsPresetFavoriteService favoriteService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await favoriteService.GetFavoriteDashboardAsync(
                        favoriteKey,
                        limit ?? 50,
                        cancellationToken);

                    return response is null
                        ? Results.NotFound(new AdminApiErrorResponse("Operational failure analytics preset favorite was not found."))
                        : Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureAnalyticsPresetFavorite")
            .WithTags("Operational Store")
            .WithSummary("Gets analytics for a built-in favorite preset group.")
            .Produces<OperationalGlobalFailureAnalyticsPresetFavoriteDashboardResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}


