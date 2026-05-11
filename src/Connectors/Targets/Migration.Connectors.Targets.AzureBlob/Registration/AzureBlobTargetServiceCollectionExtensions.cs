using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;
using Migration.Connectors.Targets.AzureBlob.Configuration;

namespace Migration.Connectors.Targets.AzureBlob.Registration;

public static class AzureBlobTargetServiceCollectionExtensions
{
    public static IServiceCollection AddAzureBlobTargetConnector(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureBlobTargetOptions>(configuration.GetSection(AzureBlobTargetOptions.SectionName));
        services.AddHttpClient(nameof(AzureBlobTargetConnector));
        services.AddSingleton<IAssetTargetConnector, AzureBlobTargetConnector>();
        return services;
    }
}
