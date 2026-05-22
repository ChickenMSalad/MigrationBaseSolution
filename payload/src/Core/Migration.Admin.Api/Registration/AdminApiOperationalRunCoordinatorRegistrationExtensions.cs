using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.Admin.Api.Registration;

public static class AdminApiOperationalRunCoordinatorRegistrationExtensions
{
    public static IServiceCollection AddAdminApiOperationalRunCoordinator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSqlOperationalRunCoordinator(configuration);
        return services;
    }
}
