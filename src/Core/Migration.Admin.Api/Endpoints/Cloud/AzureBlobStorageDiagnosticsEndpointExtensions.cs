using Migration.ControlPlane.Storage;

namespace Migration.Admin.Api.Endpoints;

public static class AzureBlobStorageDiagnosticsEndpointExtensions
{
    public static RouteGroupBuilder MapAzureBlobStorageDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/storage/azure-blob/diagnostics", (
                IConfiguration configuration,
                CloudBinaryStorageProviderCapabilities capabilities) =>
            {
                var storageRoot = configuration["ControlPlane:StorageRoot"] ?? ".migration-control-plane";
                var selectedProvider = storageRoot.StartsWith("az://", StringComparison.OrdinalIgnoreCase) ||
                                       storageRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? CloudStorageProviders.AzureBlob
                    : CloudStorageProviders.LocalFileSystem;

                var accountName = FirstNonEmpty(
                    configuration["AzureBlobStorage:AccountName"],
                    configuration["Cloud:ArtifactStorageAccountName"],
                    configuration["Cloud:QueueStorageAccountName"]);

                var serviceUriConfigured = !string.IsNullOrWhiteSpace(configuration["AzureBlobStorage:ServiceUri"]);
                var connectionStringConfigured = !string.IsNullOrWhiteSpace(configuration["AzureBlobStorage:ConnectionString"]);

                var containerName = FirstNonEmpty(
                    configuration["AzureBlobStorage:ContainerName"],
                    TryContainerFromAzRoot(storageRoot));

                var isConfigured =
                    selectedProvider != CloudStorageProviders.AzureBlob ||
                    connectionStringConfigured ||
                    serviceUriConfigured ||
                    !string.IsNullOrWhiteSpace(accountName);

                var warnings = new List<string>();

                if (selectedProvider == CloudStorageProviders.AzureBlob && string.IsNullOrWhiteSpace(containerName))
                {
                    warnings.Add("Azure Blob storage is selected but no container name could be resolved.");
                }

                if (selectedProvider == CloudStorageProviders.AzureBlob && !isConfigured)
                {
                    warnings.Add("Azure Blob storage is selected but no connection string, service URI, or account name is configured.");
                }

                return Results.Ok(new
                {
                    storageRoot,
                    selectedProvider,
                    activeProvider = capabilities.Provider,
                    azureBlobSelected = selectedProvider == CloudStorageProviders.AzureBlob,
                    azureBlobConfigured = isConfigured && selectedProvider == CloudStorageProviders.AzureBlob,
                    accountNameConfigured = !string.IsNullOrWhiteSpace(accountName),
                    serviceUriConfigured,
                    connectionStringConfigured,
                    containerName,
                    capabilities,
                    warnings
                });
            })
            .WithName("GetAzureBlobStorageDiagnostics")
            .WithTags("Cloud")
            .WithSummary("Gets safe Azure Blob storage provider diagnostics without exposing secrets.");

        return api;
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

    private static string? TryContainerFromAzRoot(string root)
    {
        if (!root.StartsWith("az://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var withoutScheme = root["az://".Length..].Trim('/');
        var firstSlash = withoutScheme.IndexOf('/');

        return firstSlash < 0
            ? withoutScheme
            : withoutScheme[..firstSlash];
    }
}
