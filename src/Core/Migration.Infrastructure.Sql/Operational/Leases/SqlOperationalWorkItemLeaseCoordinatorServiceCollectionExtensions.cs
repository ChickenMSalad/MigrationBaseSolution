using Migration.Application.Operational.Leases;
using Migration.Infrastructure.Sql.Operational.Leases;

namespace Microsoft.Extensions.DependencyInjection;

public static class SqlOperationalWorkItemLeaseCoordinatorServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalWorkItemLeaseCoordinator(this IServiceCollection services)
    {
        services.AddOptions<SqlOperationalWorkItemLeaseCoordinatorOptions>();
        services.AddSingleton<IOperationalWorkItemLeaseCoordinator, SqlOperationalWorkItemLeaseCoordinator>();
        return services;
    }

    public static IServiceCollection AddSqlOperationalWorkItemLeaseCoordinator(
        this IServiceCollection services,
        Action<SqlOperationalWorkItemLeaseCoordinatorOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IOperationalWorkItemLeaseCoordinator, SqlOperationalWorkItemLeaseCoordinator>();
        return services;
    }
}
