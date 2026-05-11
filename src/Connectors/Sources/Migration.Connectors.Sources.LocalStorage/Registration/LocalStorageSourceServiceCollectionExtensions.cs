using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;

namespace Migration.Connectors.Sources.LocalStorage.Registration;

public static class LocalStorageSourceServiceCollectionExtensions
{
    public static IServiceCollection AddLocalStorageSourceConnector(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LocalStorageSourceOptions>(configuration.GetSection("LocalStorage:Source"));
        services.AddSingleton<IAssetSourceConnector, LocalStorageSourceConnector>();
        return services;
    }
}
