using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Queues;

public static class QueueExecutionObservabilityRegistrationExtensions
{
    public static IServiceCollection AddQueueExecutionObservability(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IQueueExecutionObservabilityService,
            QueueExecutionObservabilityService>();

        return services;
    }
}
