using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Migration.Infrastructure.Sql.Connections;
using Migration.Infrastructure.Sql.Options;
using Migration.Infrastructure.Sql.Stores;

namespace Migration.Infrastructure.Sql.Registration;

public static class SqlOperationalStoreServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<SqlOperationalStoreOptions>()
            .Bind(configuration.GetSection(SqlOperationalStoreOptions.SectionName))
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<SqlOperationalStoreOptions>, SqlOperationalStoreOptionsValidator>());

        services.TryAddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.TryAddScoped<ISqlOperationalBackboneStore, SqlOperationalBackboneStore>();

        return services;
    }

    public static IServiceCollection AddSqlOperationalStore(
        this IServiceCollection services)
    {
        services
            .AddOptions<SqlOperationalStoreOptions>()
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<SqlOperationalStoreOptions>, SqlOperationalStoreOptionsValidator>());

        services.TryAddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.TryAddScoped<ISqlOperationalBackboneStore, SqlOperationalBackboneStore>();

        return services;
    }
}
