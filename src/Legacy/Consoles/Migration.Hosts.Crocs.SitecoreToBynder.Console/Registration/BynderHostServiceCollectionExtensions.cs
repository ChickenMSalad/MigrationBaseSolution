using Bynder.Sdk.Service;
using Bynder.Sdk.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Connectors.Targets.Bynder.Services;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Console.Registration;

public static class BynderHostServiceCollectionExtensions
{
    public static IServiceCollection AddBynderHostServices(this IServiceCollection services)
    {
        services.AddSingleton<IBynderClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BynderOptions>>().Value;
            return ClientFactory.Create(options.Client);
        });

        services.AddSingleton<AssetResiliencyService>();
        services.AddSingleton<MetapropertyOptionBuilderFactory>();
        services.AddSingleton<MetapropertyOptionBuilderFactoryApi>();
        services.AddSingleton<BynderMetadataPropertiesService>();
        services.AddScoped<BynderDataMigrationBatchService>();
        services.AddScoped<BynderUpdateDataService>();
        services.AddScoped<BynderReportingService>();
        services.AddSingleton<BynderHostPathResolver>();
        return services;
    }
}
