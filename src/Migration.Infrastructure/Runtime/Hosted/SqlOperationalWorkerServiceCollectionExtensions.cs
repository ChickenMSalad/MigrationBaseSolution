using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Infrastructure.Runtime.SqlServer;

namespace Migration.Infrastructure.Runtime.Hosted;

public static class SqlOperationalWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalWorkerRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<IServiceProvider, SqlOperationalWorkItemExecutor> executorFactory)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (executorFactory is null)
        {
            throw new ArgumentNullException(nameof(executorFactory));
        }

        services.Configure<SqlOperationalWorkerOptions>(configuration.GetSection(SqlOperationalWorkerOptions.SectionName));

        services.AddSingleton(provider => executorFactory(provider));
        services.AddSingleton<SqlOperationalQueueRuntime>();
        services.AddHostedService<SqlOperationalWorkerHostedService>();

        return services;
    }
}
