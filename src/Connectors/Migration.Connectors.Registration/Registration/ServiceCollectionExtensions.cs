using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Connectors.Sources.WebDam.Registration;
using Migration.Connectors.Targets.AzureBlob.Registration;
using Migration.Connectors.Targets.Bynder.Registration;

namespace Migration.Connectors.Registration;

/// <summary>
/// Central composition point for connector modules used by runtime hosts.
/// Hosts such as the queue worker should reference this project instead of directly
/// referencing every source and target connector implementation project.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationConnectorModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register only connector projects that actually exist in the solution.
        // Connector catalog entries for future systems can exist in the UI, but runtime
        // registration must not import/call modules until those projects are implemented.
        services.AddWebDamSourceConnector(configuration);
        services.AddAzureBlobTargetConnector(configuration);
        services.AddBynderTargetConnector(configuration);

        return services;
    }
}
