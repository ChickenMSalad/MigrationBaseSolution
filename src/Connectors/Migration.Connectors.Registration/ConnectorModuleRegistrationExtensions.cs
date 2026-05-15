using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Connectors.Sources.Aem.Registration;
using Migration.Connectors.Sources.WebDam.Registration;
using Migration.Connectors.Targets.AzureBlob.Registration;
using Migration.Connectors.Targets.S3.Registration;

namespace Migration.Connectors.Registration;

public static class ConnectorModuleRegistrationExtensions
{
    public static IServiceCollection AddMigrationConnectorModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddWebDamSourceConnector(configuration);
        services.AddAemSourceConnector(configuration);

        Migration.Connectors.Sources.SharePoint.Registration.SharePointSourceConnectorRegistration
            .AddSharePointSourceConnector(services, configuration);

        services.AddAzureBlobTargetConnector(configuration);
        services.AddS3TargetConnector(configuration);

        return services;
    }
}
