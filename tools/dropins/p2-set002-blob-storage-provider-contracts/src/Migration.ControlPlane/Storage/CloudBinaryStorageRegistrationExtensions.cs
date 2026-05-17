using Microsoft.Extensions.DependencyInjection;

namespace Migration.ControlPlane.Storage;

public static class CloudBinaryStorageRegistrationExtensions
{
    public static IServiceCollection AddCloudBinaryStorage(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICloudBinaryStorageProvider, NullCloudBinaryStorageProvider>();

        services.AddSingleton(new CloudBinaryStorageProviderCapabilities(
            Provider: "null",
            SupportsStreamingWrites: false,
            SupportsMultipartUploads: false,
            SupportsObjectTags: false,
            SupportsLeases: false,
            SupportsVersioning: false,
            SupportsConditionalWrites: false,
            SupportsSignedUrls: false));

        return services;
    }
}
