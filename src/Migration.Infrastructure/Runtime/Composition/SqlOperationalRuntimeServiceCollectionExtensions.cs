using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Infrastructure.Runtime.Hosted;
using Migration.Infrastructure.Runtime.SqlServer;

namespace Migration.Infrastructure.Runtime.Composition;

public static class SqlOperationalRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddP7SqlOperationalRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<IServiceProvider, CancellationToken, Task<DbConnection>> openConnection,
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

        if (openConnection is null)
        {
            throw new ArgumentNullException(nameof(openConnection));
        }

        if (executorFactory is null)
        {
            throw new ArgumentNullException(nameof(executorFactory));
        }

        services.Configure<SqlOperationalRuntimeCompositionOptions>(
            configuration.GetSection(SqlOperationalRuntimeCompositionOptions.SectionName));

        services.Configure<SqlOperationalWorkerOptions>(
            configuration.GetSection(SqlOperationalWorkerOptions.SectionName));

        services.AddSingleton<ISqlOperationalConnectionFactory>(provider =>
            new DelegateSqlOperationalConnectionFactory(cancellationToken => openConnection(provider, cancellationToken)));

        services.AddSingleton<SqlOperationalQueueStore>();
        services.AddSingleton<SqlOperationalRunStore>();
        services.AddSingleton<SqlOperationalQueueRuntime>();
        services.AddSingleton(provider => executorFactory(provider));
        services.AddSingleton<SqlOperationalRuntimeReadinessProbe>();
        services.AddHostedService<SqlOperationalWorkerHostedService>();

        return services;
    }
}
