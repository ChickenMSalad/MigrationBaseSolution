using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalGlobalFailureAnalyticsPresetEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalGlobalFailureAnalyticsPresetEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/failures/analytics-presets",
                async (
                    int? limit,
                    IOperationalGlobalFailureAnalyticsPresetService presetService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await presetService.GetPresetCatalogAsync(
                        limit ?? 50,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureAnalyticsPresets")
            .WithTags("Operational Store")
            .WithSummary("Gets operational failure analytics query presets.")
            .Produces<OperationalGlobalFailureAnalyticsPresetCatalogResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapGet(
                "/operational/failures/analytics-presets/{presetKey}",
                async (
                    string presetKey,
                    int? limit,
                    IOperationalGlobalFailureAnalyticsPresetService presetService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await presetService.GetPresetAnalyticsAsync(
                        presetKey,
                        limit ?? 50,
                        cancellationToken);

                    return response is null
                        ? Results.NotFound(new AdminApiErrorResponse("Operational failure analytics preset was not found."))
                        : Results.Ok(response);
                })
            .WithName("GetOperationalGlobalFailureAnalyticsPreset")
            .WithTags("Operational Store")
            .WithSummary("Gets operational failure analytics for a preset query.")
            .Produces<OperationalGlobalFailureAnalyticsPresetResponse>(StatusCodes.Status200OK)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return api;
    }
}


