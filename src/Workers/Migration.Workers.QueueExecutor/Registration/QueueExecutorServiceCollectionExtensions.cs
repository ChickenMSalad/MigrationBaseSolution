using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Connectors.Registration;
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

        // Shared runtime registration: manifests, mapper, validation, orchestration, state, and progress.
        services.AddGenericMigrationRuntime(configuration);
        services.AddMigrationControlPlane(configuration);

        // Concrete connector modules used by queued migration runs.
        // Keep the worker decoupled from individual connector projects by depending on the composition project only.
        services.AddMigrationConnectorModules(configuration);

        services.AddHostedService<MigrationRunQueueWorker>();

        return services;
    }
}
