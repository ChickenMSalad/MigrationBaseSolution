using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Storage;

public static class CloudStorageRegistrationExtensions
{
    public static IServiceCollection AddCloudStoragePathResolution(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);


        var root = configuration["ControlPlane:StorageRoot"] ?? "Runtime/admin-api";

        services.AddSingleton<ICloudStoragePathResolver>(_ => new CloudStoragePathResolver(root));

        return services;
    }
}
