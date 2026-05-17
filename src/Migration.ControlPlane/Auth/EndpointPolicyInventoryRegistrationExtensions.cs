using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Auth;

public static class EndpointPolicyInventoryRegistrationExtensions
{
    public static IServiceCollection AddEndpointPolicyInventory(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IEndpointPolicyInventoryService, EndpointPolicyInventoryService>();

        return services;
    }
}
