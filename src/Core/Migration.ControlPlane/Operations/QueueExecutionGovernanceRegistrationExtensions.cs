using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Operations;

public static class QueueExecutionGovernanceRegistrationExtensions
{
    public static IServiceCollection AddQueueExecutionGovernance(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IQueueExecutionGovernanceService, QueueExecutionGovernanceService>();

        return services;
    }
}
