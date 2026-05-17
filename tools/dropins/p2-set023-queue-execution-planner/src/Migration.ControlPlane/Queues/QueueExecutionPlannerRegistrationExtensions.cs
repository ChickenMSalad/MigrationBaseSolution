using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Queues;

public static class QueueExecutionPlannerRegistrationExtensions
{
    public static IServiceCollection AddQueueExecutionPlanning(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IQueueExecutionPlanner, QueueExecutionPlanner>();

        return services;
    }
}
