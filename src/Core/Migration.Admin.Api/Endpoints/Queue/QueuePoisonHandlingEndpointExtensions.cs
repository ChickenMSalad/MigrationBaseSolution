using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueuePoisonHandlingEndpointExtensions
{
    public static RouteGroupBuilder MapQueuePoisonHandlingEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/poison-handling", (
                IConfiguration configuration,
                IQueueReceiveProvider receiveProvider) =>
            {
                var options = QueuePoisonHandlingPlanner.BuildOptions(configuration);
                var plan = QueuePoisonHandlingPlanner.BuildPlan(options, receiveProvider.Descriptor);

                return Results.Ok(plan);
            })
            .WithName("GetQueuePoisonHandlingPlan")
            .WithTags("Cloud")
            .WithSummary("Gets the queue poison/dead-letter handling plan.");

        api.MapGet("/cloud/queue/poison-handling/recommendation", (
                IConfiguration configuration,
                IQueueReceiveProvider receiveProvider) =>
            {
                var options = QueuePoisonHandlingPlanner.BuildOptions(configuration);
                var plan = QueuePoisonHandlingPlanner.BuildPlan(options, receiveProvider.Descriptor);

                var recommendation = receiveProvider.Descriptor.ProviderKind.Equals("azureStorageQueue", StringComparison.OrdinalIgnoreCase)
                    ? "Use MaxAttempts with failure artifact persistence and optional separate poison queue."
                    : receiveProvider.Descriptor.ProviderKind.Equals("serviceBus", StringComparison.OrdinalIgnoreCase)
                        ? "Use native dead lettering with failure artifact persistence for auditability."
                        : "Use failure artifact persistence for local/in-memory diagnostics.";

                return Results.Ok(new
                {
                    recommendation,
                    plan
                });
            })
            .WithName("GetQueuePoisonHandlingRecommendation")
            .WithTags("Cloud")
            .WithSummary("Gets queue poison/dead-letter handling recommendations.");

        return api;
    }
}


