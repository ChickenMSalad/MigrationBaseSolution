using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Operations;

public static class ProductionSafetyGateRegistrationExtensions
{
    public static IServiceCollection AddProductionSafetyGates(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IProductionSafetyGateService, ProductionSafetyGateService>();

        return services;
    }
}
