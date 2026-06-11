using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.OperationalStore;
using Migration.ControlPlane.Registration;
using Migration.GenericRuntime.Registration;
using Migration.Infrastructure.Sql.Registration;
using Migration.Workers.QueueExecutor.Options;
using Migration.Workers.QueueExecutor.Services;

namespace Migration.Workers.QueueExecutor.Registration;

public static class QueueExecutorServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationQueueExecutor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<QueueExecutorOptions>(
            configuration.GetSection(QueueExecutorOptions.SectionName));

        services.Configure<OperationalQueuePublisherOptions>(
            configuration.GetSection(OperationalQueuePublisherOptions.SectionName));

        services.AddSingleton<IOperationalQueueMessageSerializer, OperationalQueueMessageSerializer>();

        // Shared execution/runtime path used by API and worker hosts.
        services.AddMigrationRuntime(configuration);

        // Control-plane storage, queues, project/run stores, credentials,
        // artifact helpers, progress monitoring, and manifest builders.
        services.AddMigrationControlPlane(configuration);

        // SQL operational store foundation and P3B execution-preparation services.
        // This only registers services; the worker execution path is not changed here.
        services.AddSqlOperationalStore(configuration);

        // Replace the behavior-neutral default publisher with the QueueExecutor host's
        // Azure Queue implementation. Nothing publishes through this until callers
        // explicitly use IOperationalWorkItemQueuePublisher.
        services.AddScoped<IOperationalQueuePublisher, AzureOperationalQueuePublisher>();

        // Explicitly call the connector module registration extension to avoid
        // ambiguity with similarly named extension methods in runtime projects.
        // This preserves the existing QueueExecutor behavior while keeping the
        // generic runtime composition centralized.
        Migration.Connectors.Registration.ConnectorModuleRegistrationExtensions
            .AddMigrationConnectorModules(services, configuration);

        services.AddSingleton<ProjectCredentialJobSettingsHydrator>();
        services.AddHostedService<MigrationRunQueueWorker>();

        return services;
    }
}
