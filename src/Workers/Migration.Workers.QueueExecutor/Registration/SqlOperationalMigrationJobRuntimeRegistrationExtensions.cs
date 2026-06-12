using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.ControlPlane.Registration;
using Migration.GenericRuntime.Registration;
using Migration.Infrastructure.DependencyInjection;
using Migration.Workers.QueueExecutor.Services;

namespace Migration.Workers.QueueExecutor.Registration;

public static class SqlOperationalMigrationJobRuntimeRegistrationExtensions
{
    public static IServiceCollection AddSqlOperationalMigrationJobRuntime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMigrationRuntime(configuration);
        services.AddMigrationControlPlane(configuration);
        services.AddOperationalStore();

        // Generic runtime is the single composition point for manifest providers,
        // source connectors, target connectors, mapping, validation, and orchestration.
        // Do not call AddMigrationConnectorModules here; that bypasses GenericMigrationRuntime
        // filters and can register duplicate or partially composed connectors.
        services.AddGenericMigrationRuntime(configuration);

        services.AddSingleton<ProjectCredentialJobSettingsHydrator>();

        return services;
    }
}
