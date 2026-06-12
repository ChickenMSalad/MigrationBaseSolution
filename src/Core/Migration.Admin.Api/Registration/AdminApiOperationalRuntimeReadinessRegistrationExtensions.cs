using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.Admin.Api.Registration;

public static class AdminApiOperationalRuntimeReadinessRegistrationExtensions
{
    public static IServiceCollection AddAdminApiOperationalRuntimeReadiness(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSqlOperationalRuntimeReadiness(configuration);
        return services;
    }
}


