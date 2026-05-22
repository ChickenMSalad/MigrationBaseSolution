using Migration.Application.Operational.WorkItems;
using Migration.Infrastructure.Sql.Operational.WorkItems;

namespace Microsoft.Extensions.DependencyInjection;

public static class SqlOperationalWorkItemQueueServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalWorkItemQueue(this IServiceCollection services)
    {
        services.AddOptions<SqlOperationalWorkItemQueueOptions>();
        services.AddSingleton<IOperationalWorkItemQueue, SqlOperationalWorkItemQueue>();
        return services;
    }

    public static IServiceCollection AddSqlOperationalWorkItemQueue(
        this IServiceCollection services,
        Action<SqlOperationalWorkItemQueueOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IOperationalWorkItemQueue, SqlOperationalWorkItemQueue>();
        return services;
    }
}
