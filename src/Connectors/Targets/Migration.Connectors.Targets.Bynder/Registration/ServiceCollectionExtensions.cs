using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Migration.Application.Abstractions;
using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Connectors.Targets.Bynder.Taxonomy;

namespace Migration.Connectors.Targets.Bynder.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBynderConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BynderOptions>(configuration.GetSection(BynderOptions.SectionName));
        services.AddMemoryCache();
        services.TryAddSingleton<BynderTaxonomyWorkbookBuilder>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetSourceConnector, BynderSourceConnector>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetTargetConnector, BynderTargetConnector>());

        return services;
    }

    public static IServiceCollection AddBynderSourceConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BynderOptions>(configuration.GetSection(BynderOptions.SectionName));
        services.AddMemoryCache();
        services.TryAddSingleton<BynderTaxonomyWorkbookBuilder>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetSourceConnector, BynderSourceConnector>());

        return services;
    }

    public static IServiceCollection AddBynderTargetConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BynderOptions>(configuration.GetSection(BynderOptions.SectionName));
        services.AddMemoryCache();
        services.TryAddSingleton<BynderTaxonomyWorkbookBuilder>();

        // Register the runtime connector even when appsettings has no global Bynder:Client section.
        // Control-plane queued runs hydrate BaseUrl, ClientId, ClientSecret, Scopes, and BrandStoreId
        // from the selected target credential set into MigrationJobDefinition.Settings.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetTargetConnector, BynderTargetConnector>());

        // Bynder can also act as a source. Keep this in the same connector project so we do not
        // duplicate Bynder SDK/client/auth code in a second source-specific project.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAssetSourceConnector, BynderSourceConnector>());

        return services;
    }
}
