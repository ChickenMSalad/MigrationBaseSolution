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

        services.AddSingleton<AssetResiliencyService>();
        services.AddSingleton<MetapropertyOptionBuilderFactory>();
        services.AddSingleton<IValidationStep, BynderMetadataValidationStep>();

        // Bynder's SDK client requires a globally configured Bynder:Client section.
        // Most of the control-plane flows in this solution use credential-set JSON instead,
        // so registering the runtime target connector without global config causes startup
        // failures when DI eagerly resolves connectors/validation steps.
        //
        // Only register the runtime Bynder target connector when the legacy/global config is
        // actually present. Taxonomy Builder still reads Bynder credentials from the selected
        // credential set and does not depend on this singleton connector registration.
        if (HasUsableGlobalBynderConfiguration(configuration))
        {
            services.AddSingleton<IBynderClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BynderOptions>>().Value;
                return ClientFactory.Create(options.Client);
            });

            services.AddSingleton<IAssetTargetConnector, BynderTargetConnector>();
        }

        return services;
    }

    private static bool HasUsableGlobalBynderConfiguration(IConfiguration configuration)
    {
        var clientSection = configuration
            .GetSection(BynderOptions.SectionName)
            .GetSection("Client");

        return !string.IsNullOrWhiteSpace(clientSection["BaseUrl"])
            && !string.IsNullOrWhiteSpace(clientSection["ClientId"])
            && !string.IsNullOrWhiteSpace(clientSection["ClientSecret"]);
    }
}
