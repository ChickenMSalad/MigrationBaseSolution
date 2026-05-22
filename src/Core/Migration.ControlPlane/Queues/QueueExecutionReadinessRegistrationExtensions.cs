using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Queues;

public static class QueueExecutionReadinessRegistrationExtensions
{
    public static IServiceCollection AddQueueExecutionReadiness(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IQueueExecutionReadinessService,
            QueueExecutionReadinessService>();

        return services;
    }
}
