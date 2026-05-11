using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.ControlPlane.Registration;
using Migration.GenericRuntime.Registration;
using Migration.Workers.QueueExecutor.Options;
using Migration.Workers.QueueExecutor.Services;

namespace Migration.Workers.QueueExecutor.Registration;

public static class QueueExecutorServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationQueueExecutor(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<QueueExecutorOptions>(configuration.GetSection(QueueExecutorOptions.SectionName));

        services.AddGenericMigrationRuntime(configuration);
        services.AddMigrationControlPlane(configuration);

        // Explicitly call the connector module registration extension to avoid ambiguity with
        // Migration.GenericRuntime.Registration.ServiceCollectionExtensions.AddMigrationConnectorModules(...).
        Migration.Connectors.Registration.ConnectorModuleRegistrationExtensions.AddMigrationConnectorModules(
            services,
            configuration);

        services.AddSingleton<ProjectCredentialJobSettingsHydrator>();
        services.AddHostedService<MigrationRunQueueWorker>();

        return services;
    }
}
