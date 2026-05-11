using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Bynder.Sdk.Service;
using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Connectors.Targets.Bynder.Services;
using Migration.Connectors.Targets.Bynder.Clients;

namespace Migration.Hosts.WebDamToBynder.Console.Registration;

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
        services.AddScoped<BynderWebDamMigrationService>();
        services.AddScoped<BynderS3BatchOperationsService>();
        services.AddScoped<BynderUpdateDataService>();
        services.AddScoped<BynderS3UpdateOperationsService>();
        services.AddScoped<BynderS3MetadataOperationsService>();
        services.AddScoped<BynderReportingService>();
        return services;
    }
}
