using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunCompletionFinalizationEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunCompletionFinalizationEndpoints(
        this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/runs/{runId:guid}/completion-readiness",
                async (
                    Guid runId,
                    IOperationalRunCompletionFinalizationService finalizationService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await finalizationService.GetReadinessAsync(runId, cancellationToken);

                    return response.CurrentStatus == "Unknown"
                        ? Results.NotFound(response)
                        : Results.Ok(response);
                })
            .WithName("GetOperationalRunCompletionReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets operational run completion finalization readiness.")
            .Produces<OperationalRunCompletionReadinessResponse>(StatusCodes.Status200OK)
            .Produces<OperationalRunCompletionReadinessResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        api.MapPost(
                "/operational/runs/{runId:guid}/finalize-completion",
                async (
                    Guid runId,
                    IOperationalRunCompletionFinalizationService finalizationService,
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
            .WithName("FinalizeOperationalRunCompletion")
            .WithTags("Operational Store")
            .WithSummary("Finalizes an operational run as completed when all work is complete.")
            .Produces<OperationalRunCompletionReadinessResponse>(StatusCodes.Status200OK)
            .Produces<OperationalRunCompletionReadinessResponse>(StatusCodes.Status404NotFound)
            .Produces<AdminApiErrorResponse>(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        return api;
    }
}
