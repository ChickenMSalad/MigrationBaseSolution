using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Migration.Infrastructure.State.OperationalStore.Sql;

public static class SqlOperationalStoreServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalStoreOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<SqlOperationalStoreOptions>, SqlOperationalStoreOptionsValidator>());

        services.TryAddSingleton<ISqlOperationalStoreConnectionStringResolver, SqlOperationalStoreConnectionStringResolver>();

        services
            .AddOptions<SqlOperationalStoreOptions>()
            .Bind(configuration.GetSection(SqlOperationalStoreOptions.SectionName))
            .ValidateOnStart();

        return services;
    }
}
