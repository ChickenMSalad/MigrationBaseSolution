using Microsoft.Extensions.DependencyInjection;

using Migration.Hosts.WebDamToBynder.Console.Plugins;

namespace Migration.Hosts.WebDamToBynder.Console.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlugins(this IServiceCollection services)
    {
        services.AddScoped<IPlugin, WebDamDataMigrationPlugin>();
        services.AddScoped<IPlugin, CreateBynderMetadataPropertiesFilePlugin>();
        services.AddScoped<IPlugin, CreateBynderMetadataTemplatePlugin>();
        services.AddScoped<IPlugin, BynderDataMigrationPlugin>();
        return services;
    }
}
