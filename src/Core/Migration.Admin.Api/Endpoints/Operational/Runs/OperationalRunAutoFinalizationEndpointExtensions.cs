using Migration.Admin.Api.OperationalStore;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunAutoFinalizationEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunAutoFinalizationEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/auto-finalization/status",
                (IOptions<OperationalRunAutoFinalizationOptions> options) =>
                {
                    var value = options.Value;

                    return Results.Ok(new OperationalRunAutoFinalizationStatusResponse
                    {
                        Enabled = value.Enabled,
                        IntervalSeconds = value.IntervalSeconds,
                        BatchSize = value.BatchSize,
                        Mode = value.Enabled ? "Enabled" : "Disabled"
                    });
                })
            .WithName("GetOperationalRunAutoFinalizationStatus")
            .WithTags("Operational Store")
            .WithSummary("Gets operational run auto-finalization status.")
            .Produces<OperationalRunAutoFinalizationStatusResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        api.MapPost(
                "/operational/runs/auto-finalization/run-once",
                async (
                    IOperationalRunAutoFinalizationService finalizationService,
                    CancellationToken cancellationToken) =>
                {
                    var finalizedCount = await finalizationService.FinalizeEligibleRunsAsync(
                        cancellationToken);

                    return Results.Ok(new
                    {
                        finalizedCount
                    });
                })
            .WithName("RunOperationalRunAutoFinalizationOnce")
            .WithTags("Operational Store")
            .WithSummary("Runs operational run auto-finalization once.")
            .WithOpenApi();

        return api;
    }
}


