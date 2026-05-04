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
        // Runtime connector registration only.
        //
        // Keep this list to connectors that are currently executable by the worker
        // without requiring appsettings-time global credentials. Connector catalog
        // visibility for S3, AEM, Sitecore, Cloudinary, Aprimo, and Bynder should
        // stay in the Admin/API catalog layer, not force runtime SDK clients to be
        // created during worker startup.
        services.AddWebDamSourceConnector(configuration);
        services.AddAzureBlobTargetConnector(configuration);

        return services;
    }
}
