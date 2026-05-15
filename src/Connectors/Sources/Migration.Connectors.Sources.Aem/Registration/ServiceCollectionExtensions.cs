using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;
using Migration.Connectors.Sources.Aem.Clients;
using Migration.Connectors.Sources.Aem.Configuration;
using Migration.Connectors.Sources.Aem.ManifestBuilder;
using Migration.Connectors.Sources.Aem.Services;
using Migration.ControlPlane.ManifestBuilder;

namespace Migration.Connectors.Sources.Aem.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAemSourceConnector(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AemOptions>(configuration.GetSection("Aem"));

        services.AddSingleton<IAemClient>(sp =>
        {
            var options = configuration.GetSection("Aem").Get<AemOptions>() ?? new AemOptions();
            return new AemClient(new HttpClient(), options);
        });

        services.AddSingleton<AemDataMigrationService>();
        services.AddSingleton<IAssetSourceConnector, AemSourceConnector>();
        services.AddSingleton<ISourceManifestService, AemExportFoldersManifestService>();

        return services;
    }
}
