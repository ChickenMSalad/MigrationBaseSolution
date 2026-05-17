using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Storage;

public static class CloudBinaryStorageRegistrationExtensions
{
    public static IServiceCollection AddCloudBinaryStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var root = configuration["ControlPlane:StorageRoot"] ?? ".migration-control-plane";
        var provider = IsBlobRoot(root)
            ? CloudStorageProviders.AzureBlob
            : CloudStorageProviders.LocalFileSystem;

        if (string.Equals(provider, CloudStorageProviders.LocalFileSystem, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICloudBinaryStorageProvider, LocalFileSystemCloudBinaryStorageProvider>();

            services.AddSingleton(new CloudBinaryStorageProviderCapabilities(
                Provider: CloudStorageProviders.LocalFileSystem,
                SupportsStreamingWrites: true,
                SupportsMultipartUploads: false,
                SupportsObjectTags: false,
                SupportsLeases: false,
                SupportsVersioning: false,
                SupportsConditionalWrites: false,
                SupportsSignedUrls: false));

            return services;
        }

        services.AddSingleton<ICloudBinaryStorageProvider, NullCloudBinaryStorageProvider>();

        services.AddSingleton(new CloudBinaryStorageProviderCapabilities(
            Provider: CloudStorageProviders.AzureBlob,
            SupportsStreamingWrites: false,
            SupportsMultipartUploads: false,
            SupportsObjectTags: false,
            SupportsLeases: false,
            SupportsVersioning: false,
            SupportsConditionalWrites: false,
            SupportsSignedUrls: false));

        return services;
    }

    private static bool IsBlobRoot(string root) =>
        root.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
        root.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
