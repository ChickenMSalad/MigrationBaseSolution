using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Migration.Application.Operational.ExecutionHistory;
using Migration.Application.Operational.Runs;
using Migration.Application.Operational.WorkItems;
using Migration.Infrastructure.Sql.Operational.ExecutionHistory;
using Migration.Infrastructure.Sql.Operational.Runs;
using Migration.Infrastructure.Sql.Operational.WorkItems;
using Migration.Workers.QueueExecutor.Options;
using Migration.Workers.QueueExecutor.Services;

namespace Migration.Workers.QueueExecutor.Registration;

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

        services.AddOptions<SqlOperationalRunCoordinatorOptions>()
            .Bind(configuration.GetSection("SqlOperationalRunCoordinator"));

        services.AddOptions<SqlOperationalWorkItemQueueOptions>()
            .Bind(configuration.GetSection("SqlOperationalWorkItemQueue"));

        services.AddSqlOperationalExecutionHistory(configuration);

        services.TryAddSingleton<IOperationalWorkItemQueue, SqlOperationalWorkItemQueue>();
        services.TryAddSingleton<IOperationalRunCoordinator, SqlOperationalRunCoordinator>();
        services.TryAddSingleton<ISqlOperationalWorkItemExecutor, LoggingSqlOperationalWorkItemExecutor>();

        services.AddHostedService<SqlOperationalWorkItemWorker>();

        return services;
    }
}