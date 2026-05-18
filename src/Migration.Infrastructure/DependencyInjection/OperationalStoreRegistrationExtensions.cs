using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.OperationalStore;
using Migration.Infrastructure.State.OperationalStore.Sql;
using Migration.Infrastructure.State.OperationalStore.Sql.Health;
using Migration.Infrastructure.State.OperationalStore.Sql.Stores;
using Migration.Infrastructure.State.OperationalStore.Sql.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.Infrastructure.DependencyInjection;

public static class OperationalStoreRegistrationExtensions
{
    public static IServiceCollection AddOperationalStore(
        this IServiceCollection services)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        services.AddScoped<IMigrationRunStore, SqlMigrationRunStore>();
        services.AddScoped<IMigrationManifestStore, SqlMigrationManifestStore>();
        services.AddScoped<IMigrationWorkItemStore, SqlMigrationWorkItemStore>();
        services.AddScoped<IMigrationFailureStore, SqlMigrationFailureStore>();
        services.AddScoped<IMigrationCheckpointStore, SqlMigrationCheckpointStore>();
        services.AddScoped<IMigrationIdentifierMapStore, SqlMigrationIdentifierMapStore>();

        services.AddScoped<IOperationalStore, SqlOperationalStore>();

        services.AddScoped<IOperationalRunLifecycleService, OperationalRunLifecycleService>();
        services.AddScoped<IOperationalManifestLifecycleService, OperationalManifestLifecycleService>();
        services.AddScoped<IOperationalWorkItemLifecycleService, OperationalWorkItemLifecycleService>();
        services.AddScoped<IOperationalWorkItemDispatchService, OperationalWorkItemDispatchService>();
        services.AddScoped<IOperationalManifestDispatchService, OperationalManifestDispatchService>();
        services.AddScoped<IOperationalRunDispatchService, OperationalRunDispatchService>();

        services.AddScoped<IOperationalExecutionContextFactory, OperationalExecutionContextFactory>();
        services.AddScoped<IOperationalQueueMessageFactory, OperationalQueueMessageFactory>();
        services.AddScoped<IOperationalWorkItemExecutionSynchronizer, OperationalWorkItemExecutionSynchronizer>();

        services.AddSingleton<IOperationalQueueMessageSerializer, OperationalQueueMessageSerializer>();

        services.AddScoped<IOperationalQueuePublisher, NullOperationalQueuePublisher>();
        services.AddScoped<IOperationalWorkItemQueuePublisher, OperationalWorkItemQueuePublisher>();

        services.AddSingleton<IOperationalStoreSchemaValidator, OperationalStoreSchemaValidator>();
        services.AddSingleton<OperationalStoreHealthCheck>();

        return services;
    }
}
