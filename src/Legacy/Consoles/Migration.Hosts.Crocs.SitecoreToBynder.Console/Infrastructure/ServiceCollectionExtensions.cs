using Microsoft.Extensions.DependencyInjection;
using Migration.Hosts.Crocs.SitecoreToBynder.Console.Plugins;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Console.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlugins(this IServiceCollection services)
    {
        services.AddScoped<IPlugin, SitecoreMigrationPlugin>();
        services.AddScoped<IPlugin, CreateBynderMetadataPropertiesFilePlugin>();
        services.AddScoped<IPlugin, CreateBynderMetadataTemplatePlugin>();
        services.AddScoped<IPlugin, BynderDataMigrationPlugin>();
        return services;
    }
}
