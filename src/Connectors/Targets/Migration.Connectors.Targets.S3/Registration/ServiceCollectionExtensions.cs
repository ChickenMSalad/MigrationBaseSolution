using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Abstractions;
using Migration.Connectors.Targets.S3.Configuration;
using Migration.Shared.Storage.S3;

namespace Migration.Connectors.Targets.S3.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddS3TargetConnector(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<S3TargetOptions>(configuration.GetSection("S3Target"));
        services.AddSharedS3Storage();
        services.AddSingleton<IAssetTargetConnector, S3TargetConnector>();
        return services;
    }
}
