using Bynder.Sdk.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Connectors.Targets.Bynder.Services;
using Migration.Connectors.Targets.Bynder.Validation;

namespace Migration.Connectors.Targets.Bynder.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBynderTargetConnector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BynderOptions>(configuration.GetSection(BynderOptions.SectionName));
        services.AddMemoryCache();

        services.AddSingleton<IBynderClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BynderOptions>>().Value;
            return ClientFactory.Create(options.Client);
        });

        services.AddSingleton<AssetResiliencyService>();
        services.AddSingleton<MetapropertyOptionBuilderFactory>();
        services.AddSingleton<IValidationStep, BynderMetadataValidationStep>();
        services.AddSingleton<IAssetTargetConnector, BynderTargetConnector>();

        return services;
    }
}
