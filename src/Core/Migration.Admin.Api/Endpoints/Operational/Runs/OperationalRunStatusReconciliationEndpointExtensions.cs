using Migration.Admin.Api.Contracts;
using Migration.Admin.Api.OperationalStore;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalRunStatusReconciliationEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalRunStatusReconciliationEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/operational/runs/{runId:guid}/status-reconciliation", async (
                Guid runId,
                IOperationalRunStatusReconciliationService reconciliationService,
                CancellationToken cancellationToken) =>
            {
                var response = await reconciliationService.PreviewAsync(runId, cancellationToken);
                return response.CurrentStatus == "Unknown" ? Results.NotFound(response) : Results.Ok(response);
            })
            .WithName("PreviewOperationalRunStatusReconciliation")
            .WithTags("Operational Store")
            .WithOpenApi();

        api.MapPost("/operational/runs/{runId:guid}/reconcile-status", async (
                Guid runId,
                IOperationalRunStatusReconciliationService reconciliationService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var response = await reconciliationService.ApplyAsync(runId, cancellationToken);
                    return response.CurrentStatus == "Unknown" ? Results.NotFound(response) : Results.Ok(response);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new AdminApiErrorResponse(ex.Message));
                }
            })
            .WithName("ApplyOperationalRunStatusReconciliation")
            .WithTags("Operational Store")
            .WithOpenApi();

        return api;
    }
}


