using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.ControlPlane.Registration;
using Migration.GenericRuntime.Registration;
using Migration.Workers.QueueExecutor.Options;

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

        services.AddMigrationControlPlane(configuration);
        services.AddGenericMigrationRuntime(configuration);

        return services;
    }
}
