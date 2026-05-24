using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Infrastructure.Sql.Operational.Health;
using Migration.Infrastructure.Sql.Operational.Readiness;

namespace Migration.Admin.Api.Registration;

public static class AdminApiOperationalHealthProbeRegistrationExtensions
{
    public static IServiceCollection AddAdminApiOperationalHealthProbes(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSqlOperationalRuntimeReadiness(configuration);
        services.AddSqlOperationalHealthProbes();

        return services;
    }
}
