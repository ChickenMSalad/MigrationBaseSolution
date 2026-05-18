using Migration.Application.Abstractions.OperationalStore;
using Migration.Infrastructure.State.OperationalStore.Sql;
using Migration.Infrastructure.State.OperationalStore.Sql.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.Infrastructure.DependencyInjection;

public static class OperationalStoreRegistrationExtensions
{
    public static IServiceCollection AddOperationalStore(
        this IServiceCollection services)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IMigrationRunStore, SqlMigrationRunStore>();

        return services;
    }
}
