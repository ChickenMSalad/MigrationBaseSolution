using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Connectors.Sources.WebDam.Registration;
using Migration.Connectors.Targets.AzureBlob.Registration;

namespace Migration.Connectors.Registration;

/// <summary>
/// Central composition point for connector modules used by runtime hosts.
///
/// Hosts such as the queue worker should reference this project instead of directly
/// referencing every source and target connector implementation project.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationConnectorModules(this IServiceCollection services, IConfiguration configuration)
    {
        // Register concrete connector modules here.
        // This keeps host projects decoupled from individual connector implementations.
        services.AddWebDamSourceConnector(configuration);
        services.AddAzureBlobTargetConnector(configuration);

        return services;
    }
}
