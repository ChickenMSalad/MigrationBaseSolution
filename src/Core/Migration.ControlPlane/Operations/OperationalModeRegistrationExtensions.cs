using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Operations;

public static class OperationalModeRegistrationExtensions
{
    public static IServiceCollection AddOperationalMode(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IOperationalModeService, OperationalModeService>();

        return services;
    }
}
