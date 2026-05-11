using Microsoft.Extensions.DependencyInjection;

namespace Migration.Shared.Storage.S3;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedS3Storage(this IServiceCollection services)
    {
        services.AddSingleton<IS3ClientFactory, S3ClientFactory>();
        return services;
    }
}
