using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueWorkerLoopDiagnosticsEndpointExtensions
{
    public static RouteGroupBuilder MapQueueWorkerLoopDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/worker-loop", (
                IConfiguration configuration,
                IQueueReceiveProvider receiveProvider) =>
            {
                var options = QueueWorkerLoopPlanner.BuildOptions(configuration);
                var descriptor = QueueWorkerLoopPlanner.BuildDescriptor(options, receiveProvider.Descriptor);

                return Results.Ok(descriptor);
            })
            .WithName("GetQueueWorkerLoopPlan")
            .WithTags("Cloud")
            .WithSummary("Gets the queue worker polling loop configuration plan.");

        api.MapGet("/cloud/queue/worker-loop/safety", (
                IConfiguration configuration,
                IQueueReceiveProvider receiveProvider) =>
            {
                var options = QueueWorkerLoopPlanner.BuildOptions(configuration);
                var descriptor = QueueWorkerLoopPlanner.BuildDescriptor(options, receiveProvider.Descriptor);

                var canRun = descriptor.Enabled &&
                             descriptor.ReceiveProviderConfigured &&
                             !descriptor.DryRun;

                return Results.Ok(new
                {
                    canRun,
                    safeToStart = descriptor.Enabled && descriptor.ReceiveProviderConfigured,
                    requiresExplicitEnablement = !descriptor.Enabled,
                    requiresProviderConfiguration = !descriptor.ReceiveProviderConfigured,
                    willExecuteRuns = descriptor.Enabled && !descriptor.DryRun,
                    willCompleteMessages = options.CompleteMessages,
                    descriptor
                });
            })
            .WithName("GetQueueWorkerLoopSafety")
            .WithTags("Cloud")
            .WithSummary("Gets safety checks for the queue worker polling loop.");

        return api;
    }
}


