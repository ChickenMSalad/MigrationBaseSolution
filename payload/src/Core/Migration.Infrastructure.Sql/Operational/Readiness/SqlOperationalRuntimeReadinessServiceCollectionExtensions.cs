using Microsoft.Extensions.Configuration;
using Migration.Application.Operational.Readiness;
using Migration.Infrastructure.Sql.Operational.Readiness;

namespace Microsoft.Extensions.DependencyInjection;

public static class SqlOperationalRuntimeReadinessServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalRuntimeReadiness(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SqlOperationalRuntimeReadinessOptions>()
            .Bind(configuration.GetSection(SqlOperationalRuntimeReadinessOptions.SectionName));

        services.AddSingleton<IOperationalRuntimeReadinessService, SqlOperationalRuntimeReadinessService>();
        return services;
    }

    public static IServiceCollection AddSqlOperationalRuntimeReadiness(
        this IServiceCollection services,
        Action<SqlOperationalRuntimeReadinessOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddSingleton<IOperationalRuntimeReadinessService, SqlOperationalRuntimeReadinessService>();
        return services;
    }
}
