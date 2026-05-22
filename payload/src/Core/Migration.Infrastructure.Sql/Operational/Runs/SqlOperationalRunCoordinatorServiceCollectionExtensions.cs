using Microsoft.Extensions.Configuration;
using Migration.Application.Operational.Runs;
using Migration.Application.Operational.WorkItems;
using Migration.Infrastructure.Sql.Operational.Runs;
using Migration.Infrastructure.Sql.Operational.WorkItems;

namespace Microsoft.Extensions.DependencyInjection;

public static class SqlOperationalRunCoordinatorServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalRunCoordinator(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SqlOperationalRunCoordinatorOptions>()
            .Bind(configuration.GetSection("SqlOperationalRunCoordinator"));

        services.AddOptions<SqlOperationalWorkItemQueueOptions>()
            .Bind(configuration.GetSection("SqlOperationalWorkItemQueue"));

        services.AddSingleton<IOperationalWorkItemQueue, SqlOperationalWorkItemQueue>();
        services.AddSingleton<IOperationalRunCoordinator, SqlOperationalRunCoordinator>();
        return services;
    }

    public static IServiceCollection AddSqlOperationalRunCoordinator(
        this IServiceCollection services,
        Action<SqlOperationalRunCoordinatorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddSqlOperationalWorkItemQueue();
        services.AddSingleton<IOperationalRunCoordinator, SqlOperationalRunCoordinator>();
        return services;
    }
}
