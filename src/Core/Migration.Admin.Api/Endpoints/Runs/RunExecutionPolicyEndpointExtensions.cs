using Migration.ControlPlane.Models;
using Migration.ControlPlane.Services;

namespace Migration.Admin.Api.Endpoints;

public static class RunExecutionPolicyEndpointExtensions
{
    public static RouteGroupBuilder MapRunExecutionPolicyEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/runs/{runId}/execution-policy", async (
                string runId,
                IAdminProjectStore store,
                CancellationToken cancellationToken) =>
            {
                var run = await store.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);

                return run is null
                    ? Results.NotFound()
                    : Results.Ok(RunExecutionPolicyBuilder.Build(run));
            })
            .WithName("GetRunExecutionPolicy")
            .WithTags("Runs")
            .WithSummary("Gets cloud-worker execution policy metadata for a migration run.")
            .Produces<RunExecutionPolicyDescriptor>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return api;
    }
}
