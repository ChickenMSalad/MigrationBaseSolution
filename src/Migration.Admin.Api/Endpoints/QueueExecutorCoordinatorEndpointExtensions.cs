using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueExecutorCoordinatorEndpointExtensions
{
    public static RouteGroupBuilder MapQueueExecutorCoordinatorEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/executor-coordinator/options", (
                IConfiguration configuration) =>
            {
                var options = QueueExecutorCoordinatorRegistrationExtensions.BuildOptions(configuration);
                return Results.Ok(options);
            })
            .WithName("GetQueueExecutorCoordinatorOptions")
            .WithTags("Cloud")
            .WithSummary("Gets queue executor coordinator options.");

        api.MapPost("/cloud/queue/executor-coordinator/probe", async (
                IConfiguration configuration,
                IQueueExecutorCoordinator coordinator,
                CancellationToken cancellationToken) =>
            {
                var options = QueueExecutorCoordinatorRegistrationExtensions.BuildOptions(configuration);
                var safeOptions = options with
                {
                    DryRun = true,
                    CompleteMessages = false,
                    MaxMessages = Math.Min(options.MaxMessages, 1)
                };

                var result = await coordinator.PollOnceAsync(
                    safeOptions,
                    cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    options = safeOptions,
                    result
                });
            })
            .WithName("ProbeQueueExecutorCoordinator")
            .WithTags("Cloud")
            .WithSummary("Runs one safe dry-run queue executor coordinator poll.");

        return api;
    }
}
