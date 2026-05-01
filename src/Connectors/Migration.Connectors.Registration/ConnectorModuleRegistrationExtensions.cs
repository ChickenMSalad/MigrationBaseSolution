using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Connectors.Sources.WebDam.Registration;
using Migration.Connectors.Targets.AzureBlob.Registration;

namespace Migration.Connectors.Registration;

public static class ConnectorModuleRegistrationExtensions
{
    public static IServiceCollection AddMigrationConnectorModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddWebDamSourceConnector(configuration);
        services.AddAzureBlobTargetConnector(configuration);

        return services;
    }
}
