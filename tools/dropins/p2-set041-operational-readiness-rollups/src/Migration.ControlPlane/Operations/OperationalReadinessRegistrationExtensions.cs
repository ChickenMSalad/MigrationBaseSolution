using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Operations;

public static class OperationalReadinessRegistrationExtensions
{
    public static IServiceCollection AddOperationalReadiness(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IOperationalReadinessService, OperationalReadinessService>();

        return services;
    }
}
