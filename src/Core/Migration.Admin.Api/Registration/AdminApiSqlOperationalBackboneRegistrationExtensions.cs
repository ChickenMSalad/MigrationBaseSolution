using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.Admin.Api.Registration;

public static class AdminApiSqlOperationalBackboneRegistrationExtensions
{
    public static IServiceCollection AddAdminApiSqlOperationalBackbone(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // P4.2 intentionally keeps the Admin API facade thin. The durable SQL implementation
        // remains isolated in Migration.Infrastructure.Sql; operational API composition can
        // bind concrete SQL-backed stores in the next set after the facade compiles cleanly.
        return services;
    }
}
