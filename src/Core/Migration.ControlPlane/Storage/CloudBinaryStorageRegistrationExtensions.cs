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

        var options = BuildAzureBlobOptions(configuration, root);
        services.AddSingleton(options);

        if (options.IsConfigured)
        {
            services.AddSingleton<ICloudBinaryStorageProvider, AzureBlobCloudBinaryStorageProvider>();
        }
        else
        {
            services.AddSingleton<ICloudBinaryStorageProvider, NullCloudBinaryStorageProvider>();
        }

        services.AddSingleton(new CloudBinaryStorageProviderCapabilities(
            Provider: CloudStorageProviders.AzureBlob,
            SupportsStreamingWrites: true,
            SupportsMultipartUploads: false,
            SupportsObjectTags: true,
            SupportsLeases: true,
            SupportsVersioning: false,
            SupportsConditionalWrites: true,
            SupportsSignedUrls: false));

        return services;
    }

    private static AzureBlobStorageOptions BuildAzureBlobOptions(
        IConfiguration configuration,
        string root)
    {
        var containerName = configuration["AzureBlobStorage:ContainerName"];

        if (string.IsNullOrWhiteSpace(containerName) &&
            root.StartsWith("az://", StringComparison.OrdinalIgnoreCase))
        {
            var withoutScheme = root["az://".Length..].Trim('/');
            var firstSlash = withoutScheme.IndexOf('/');

            containerName = firstSlash < 0
                ? withoutScheme
                : withoutScheme[..firstSlash];
        }

        return new AzureBlobStorageOptions
        {
            AccountName = FirstNonEmpty(
                configuration["AzureBlobStorage:AccountName"],
                configuration["Cloud:ArtifactStorageAccountName"],
                configuration["Cloud:QueueStorageAccountName"]),
            ServiceUri = configuration["AzureBlobStorage:ServiceUri"],
            ConnectionString = configuration["AzureBlobStorage:ConnectionString"],
            ContainerName = containerName,
            UseManagedIdentity = ReadBool(configuration, "AzureBlobStorage:UseManagedIdentity", true)
        };
    }

    private static bool IsBlobRoot(string root) =>
        root.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
        root.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static bool ReadBool(
        IConfiguration configuration,
        string key,
        bool fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
