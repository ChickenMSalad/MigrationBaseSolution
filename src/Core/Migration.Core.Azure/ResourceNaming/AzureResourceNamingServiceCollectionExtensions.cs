using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.Core.Azure.ResourceNaming;

public static class AzureResourceNamingServiceCollectionExtensions
{
    public static IServiceCollection AddAzureResourceNaming(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AzureResourceNamingOptions>(configuration.GetSection(AzureResourceNamingOptions.SectionName));
        services.Configure<AzureResourceTagOptions>(configuration.GetSection(AzureResourceTagOptions.SectionName));
        services.AddSingleton<IAzureResourceNameBuilder, AzureResourceNameBuilder>();
        services.AddSingleton<IAzureResourceTagBuilder, AzureResourceTagBuilder>();

        return services;
    }
}
