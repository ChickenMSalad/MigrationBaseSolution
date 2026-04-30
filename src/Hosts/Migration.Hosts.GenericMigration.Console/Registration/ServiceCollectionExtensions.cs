using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.GenericRuntime.Registration;
using Migration.Hosts.GenericMigration.Console.Infrastructure;

namespace Migration.Hosts.GenericMigration.Console.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGenericMigrationConsole(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<GenericMigrationMenu>();
        services.AddSingleton<JobDefinitionLoader>();

        // Shared concrete runtime registration now lives in Migration.GenericRuntime so API/worker
        // processes do not need to reference this console host project.
        services.AddGenericMigrationRuntime(configuration, includeConsoleProgress: true);

        return services;
    }
}
