using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MigrationBase.Core.Cloud.Azure;

public static class AzureRuntimeServiceCollectionExtensions
{
    /// <summary>
    /// Registers P5 Azure runtime topology options without activating any worker, queue, SQL, storage, or telemetry runtime behavior.
    /// </summary>
    public static IServiceCollection AddAzureRuntimeTopology(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AzureRuntimeOptions>()
            .Bind(configuration.GetSection(AzureRuntimeOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AzureRuntimeOptions>, AzureRuntimeOptionsValidator>();

        return services;
    }
}
