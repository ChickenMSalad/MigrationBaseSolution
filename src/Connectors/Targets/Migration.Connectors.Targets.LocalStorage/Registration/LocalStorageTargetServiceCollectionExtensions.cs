using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;

namespace Migration.Connectors.Targets.LocalStorage.Registration;

public static class LocalStorageTargetServiceCollectionExtensions
{
    public static IServiceCollection AddLocalStorageTargetConnector(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LocalStorageTargetOptions>(configuration.GetSection("LocalStorage:Target"));
        services.AddSingleton<IAssetTargetConnector, LocalStorageTargetConnector>();
        return services;
    }
}
