using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;
using Migration.Connectors.Sources.SharePoint.Configuration;
using Migration.Connectors.Sources.SharePoint.Graph;
using Migration.Connectors.Sources.SharePoint.ManifestBuilder;
using Migration.Connectors.Sources.SharePoint.Rclone;
using Migration.ControlPlane.ManifestBuilder;

namespace Migration.Connectors.Sources.SharePoint.Registration;

public static class SharePointSourceConnectorRegistration
{
    public static IServiceCollection AddSharePointSourceConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SharePointSourceOptions>(
            configuration.GetSection("SharePointSource"));

        services.AddHttpClient();

        services.AddSingleton<RcloneProcessRunner>();
        services.AddSingleton<RcloneSharePointSourceService>();
        services.AddSingleton<GraphSharePointSourceService>();

        services.AddSingleton<SharePointRcloneManifestRunner>();
        services.AddSingleton<ISourceManifestService, SharePointRcloneManifestService>();

        services.AddSingleton<ISharePointManifestBuilder, SharePointManifestBuilder>();
        services.AddSingleton<IAssetSourceConnector, SharePointSourceConnector>();

        return services;
    }
}
