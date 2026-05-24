using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Migration.Workers.QueueExecutor.Options;
using Migration.Workers.QueueExecutor.Services;

namespace Migration.Workers.QueueExecutor.Registration;

public static class SqlOperationalMigrationJobExecutorServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalMigrationJobWorkItemExecutor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SqlOperationalMigrationJobExecutorOptions>()
            .Bind(configuration.GetSection(SqlOperationalMigrationJobExecutorOptions.SectionName));

        services.RemoveAll<ISqlOperationalWorkItemExecutor>();
        services.AddSingleton<ISqlOperationalWorkItemExecutor, SqlOperationalMigrationJobWorkItemExecutor>();

        return services;
    }
}
