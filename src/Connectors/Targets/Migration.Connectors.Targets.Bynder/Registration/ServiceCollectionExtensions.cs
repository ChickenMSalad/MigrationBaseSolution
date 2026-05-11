using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;
using Migration.Connectors.Targets.Bynder.Configuration;

namespace Migration.Connectors.Targets.Bynder.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBynderTargetConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BynderOptions>(configuration.GetSection(BynderOptions.SectionName));
        services.AddMemoryCache();

        // Register the runtime connector even when appsettings has no global Bynder:Client section.
        // Control-plane queued runs hydrate BaseUrl, ClientId, ClientSecret, Scopes, and BrandStoreId
        // from the selected target credential set into MigrationJobDefinition.Settings.
        services.AddSingleton<IAssetTargetConnector, BynderTargetConnector>();

        return services;
    }
}
