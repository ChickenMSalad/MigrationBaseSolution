using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Migration.Workers.QueueExecutor.Options;
using Migration.Workers.QueueExecutor.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class SqlOperationalQueueExecutorServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalQueueExecutor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SqlOperationalQueueExecutorOptions>()
            .Bind(configuration.GetSection(SqlOperationalQueueExecutorOptions.SectionName));

        services.AddSqlOperationalRunCoordinator(configuration);
        services.TryAddSingleton<ISqlOperationalWorkItemExecutor, LoggingSqlOperationalWorkItemExecutor>();
        services.AddHostedService<SqlOperationalWorkItemWorker>();

        return services;
    }
}
