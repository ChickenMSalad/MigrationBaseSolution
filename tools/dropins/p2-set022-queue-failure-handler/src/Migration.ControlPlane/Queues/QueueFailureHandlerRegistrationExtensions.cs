using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Queues;

public static class QueueFailureHandlerRegistrationExtensions
{
    public static IServiceCollection AddQueueFailureHandling(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IQueueFailureHandler, QueueFailureHandler>();

        return services;
    }
}
