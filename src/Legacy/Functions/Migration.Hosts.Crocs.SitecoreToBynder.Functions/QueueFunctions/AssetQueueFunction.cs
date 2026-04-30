using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Sources.Sitecore.Services;
using Migration.Connectors.Targets.Bynder.Services;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Functions.QueueFunctions;

public sealed class AssetQueueFunction(
    BlobServiceClient blobServiceClient,
    ContentHubDataMigrationService contentHubDataMigrationService,
    BynderDataMigrationBatchService bynderDataMigrationBatchService,
    BynderUpdateDataService bynderUpdateDataService,
    ILogger<AssetQueueFunction> logger)
{
    [Function(nameof(AssetQueueFunction))]
    public async Task Run([QueueTrigger("sitecore-bynder-jobs", Connection = "AzureWebJobsStorage")] string message)
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new InvalidOperationException("AzureWebJobsStorage is required.");

        var blobName = Uri.TryCreate(message, UriKind.Absolute, out var uri)
            ? Path.GetFileName(uri.LocalPath)
            : message;
        var take = GetTakeValue(blobName);
        var blobClient = blobServiceClient.GetBlobContainerClient("queuejobs").GetBlobClient(blobName);
        if (!await blobClient.ExistsAsync())
        {
            logger.LogWarning("Queue job blob not found: {BlobName}", blobName);
            return;
        }

        var download = await blobClient.DownloadContentAsync();
        using var stream = download.Value.Content.ToStream();

        if (blobName.StartsWith("bynder_", StringComparison.OrdinalIgnoreCase))
        {
            await bynderDataMigrationBatchService.UploadAssetsFromMetadata(blobName, stream, take);
        }
        else if (blobName.StartsWith("update_bynder_", StringComparison.OrdinalIgnoreCase))
        {
            var tableName = SanitizeTableName(Path.GetFileNameWithoutExtension(blobName));
            var tableService = new TableServiceClient(connectionString);
            await tableService.CreateTableIfNotExistsAsync(tableName);
            var tableClient = tableService.GetTableClient(tableName);
            await bynderUpdateDataService.UpdateAssetsFromMetadata(blobName, stream, tableClient, take);
        }
        else
        {
            await contentHubDataMigrationService.ProcessBatchesIntoAzureAsync(stream, take);
        }

        await blobClient.DeleteIfExistsAsync();
    }

    private static int GetTakeValue(string fileName)
    {
        var marker = System.Text.RegularExpressions.Regex.Match(fileName, @"_(\d+)(?=\.xlsx?$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return marker.Success && int.TryParse(marker.Groups[1].Value, out var take) ? take : 50;
    }

    private static string SanitizeTableName(string value)
    {
        var sanitized = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (sanitized.Length < 3) sanitized = sanitized.PadRight(3, 'x');
        return sanitized.Length > 63 ? sanitized[..63] : sanitized;
    }
}
