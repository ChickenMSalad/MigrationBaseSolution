using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunFailureFinalizationEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunFailureFinalizationEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/{runId:guid}/failure-readiness",
                async (
                    Guid runId,
                    IOperationalRunFailureFinalizationService finalizationService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await finalizationService.GetReadinessAsync(runId, cancellationToken);

                    return response.CurrentStatus == "Unknown"
                        ? Results.NotFound(response)
                        : Results.Ok(response);
                })
            .WithName("GetOperationalRunFailureReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets operational run failure finalization readiness.")
            .Produces<OperationalRunFailureReadinessResponse>(StatusCodes.Status200OK)
            .Produces<OperationalRunFailureReadinessResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        api.MapPost(
                "/operational/runs/{runId:guid}/finalize-failure",
                async (
                    Guid runId,
                    IOperationalRunFailureFinalizationService finalizationService,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var response = await finalizationService.FinalizeAsync(runId, cancellationToken);

                        return response.CurrentStatus == "Unknown"
                            ? Results.NotFound(response)
                            : Results.Ok(response);
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                    }
                })
            .WithName("FinalizeOperationalRunFailure")
            .WithTags("Operational Store")
            .WithSummary("Finalizes an operational run as failed when failed work exists and no work is outstanding.")
            .Produces<OperationalRunFailureReadinessResponse>(StatusCodes.Status200OK)
            .Produces<OperationalRunFailureReadinessResponse>(StatusCodes.Status404NotFound)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return api;
    }
}
