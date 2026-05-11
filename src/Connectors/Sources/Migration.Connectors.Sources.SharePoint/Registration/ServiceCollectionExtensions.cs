using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;
using Migration.Connectors.Sources.SharePoint.ManifestBuilder;
using Migration.ControlPlane.ManifestBuilder;

namespace Migration.Connectors.Sources.SharePoint.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharePointSourceConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IAssetSourceConnector, SharePointSourceConnector>();
        services.AddSingleton<SharePointRcloneManifestRunner>();
        services.AddSingleton<ISourceManifestService, SharePointRcloneManifestService>();

        return services;
    }
}
