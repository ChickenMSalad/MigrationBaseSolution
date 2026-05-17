using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Migration.ControlPlane.Storage;

public sealed class AzureBlobCloudBinaryStorageProvider : ICloudBinaryStorageProvider
{
    private readonly AzureBlobStorageOptions _options;

    public AzureBlobCloudBinaryStorageProvider(AzureBlobStorageOptions options)
    {
        _options = options;
    }

    public async Task<bool> ExistsAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default)
    {
        var blob = CreateBlobClient(location);
        var response = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    public async Task<Stream> OpenReadAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default)
    {
        var blob = CreateBlobClient(location);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.Content;
    }

    public async Task WriteAsync(
        CloudStorageLocation location,
        Stream content,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        var blob = CreateBlobClient(location);

        var headers = new BlobHttpHeaders
        {
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType
        };

        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = headers
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(
        CloudStorageLocation location,
        CancellationToken cancellationToken = default)
    {
        var blob = CreateBlobClient(location);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private BlobClient CreateBlobClient(CloudStorageLocation location)
    {
        if (!string.Equals(location.Provider, CloudStorageProviders.AzureBlob, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Azure Blob provider cannot handle storage provider '{location.Provider}'.");
        }

        var containerName = ResolveContainerName(location);
        var blobName = ResolveBlobName(location);

        BlobContainerClient container;

        if (!string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            container = new BlobContainerClient(_options.ConnectionString, containerName);
        }
        else
        {
            var serviceUri = ResolveServiceUri();
            var service = new BlobServiceClient(serviceUri, new DefaultAzureCredential());
            container = service.GetBlobContainerClient(containerName);
        }

        return container.GetBlobClient(blobName);
    }

    private Uri ResolveServiceUri()
    {
        if (!string.IsNullOrWhiteSpace(_options.ServiceUri))
        {
            return new Uri(_options.ServiceUri);
        }

        if (!string.IsNullOrWhiteSpace(_options.AccountName))
        {
            return new Uri($"https://{_options.AccountName}.blob.core.windows.net/");
        }

        throw new InvalidOperationException("Azure Blob storage is selected but no ServiceUri, AccountName, or ConnectionString is configured.");
    }

    private string ResolveContainerName(CloudStorageLocation location)
    {
        if (!string.IsNullOrWhiteSpace(_options.ContainerName))
        {
            return _options.ContainerName;
        }

        if (location.Root.StartsWith("az://", StringComparison.OrdinalIgnoreCase))
        {
            var withoutScheme = location.Root["az://".Length..].Trim('/');
            var firstSlash = withoutScheme.IndexOf('/');

            return firstSlash < 0
                ? withoutScheme
                : withoutScheme[..firstSlash];
        }

        throw new InvalidOperationException("Azure Blob container name could not be resolved.");
    }

    private static string ResolveBlobName(CloudStorageLocation location)
    {
        if (location.Root.StartsWith("az://", StringComparison.OrdinalIgnoreCase))
        {
            var withoutScheme = location.Root["az://".Length..].Trim('/');
            var firstSlash = withoutScheme.IndexOf('/');

            if (firstSlash >= 0)
            {
                var rootPrefix = withoutScheme[(firstSlash + 1)..].Trim('/');
                return string.IsNullOrWhiteSpace(rootPrefix)
                    ? location.RelativePath
                    : $"{rootPrefix}/{location.RelativePath}";
            }
        }

        return location.RelativePath;
    }
}
