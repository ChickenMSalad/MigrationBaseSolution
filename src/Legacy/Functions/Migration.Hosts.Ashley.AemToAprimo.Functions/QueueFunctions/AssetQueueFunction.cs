using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Sources.Aem.Services;
using Migration.Hosts.Ashley.AemToAprimo.Functions.Models;

namespace Migration.Hosts.Ashley.AemToAprimo.Functions.QueueFunctions;

public sealed class AssetQueueFunction(
    BlobServiceClient blobServiceClient,
    AemDataMigrationService aemDataMigrationService,
    ILogger<AssetQueueFunction> logger)
{
    [Function(nameof(AssetQueueFunction))]
    public async Task Run([QueueTrigger("aem-jobs", Connection = "AzureWebJobsStorage")] string message)
    {
        string blobName = ExtractBlobName(message);
        if (string.IsNullOrWhiteSpace(blobName))
        {
            logger.LogWarning("Queue message could not be resolved to a blob name: {Message}", message);
            return;
        }

        var take = GetTakeValue(blobName);
        var importsBlob = blobServiceClient.GetBlobContainerClient("aem-migration-contectbkp").GetBlobClient($"imports/{blobName}");
        var queueJobsBlob = blobServiceClient.GetBlobContainerClient("queuejobs").GetBlobClient(blobName);
        var blobClient = await importsBlob.ExistsAsync() ? importsBlob : queueJobsBlob;

        if (!await blobClient.ExistsAsync())
        {
            logger.LogError("Blob does not exist for {BlobName}", blobName);
            return;
        }

        var download = await blobClient.DownloadContentAsync();
        using var stream = download.Value.Content.ToStream();

        if (blobClient == importsBlob)
        {
            await aemDataMigrationService.ProcessBatchesIntoAzureAsync(blobName, stream, take);
        }
        else
        {
            await aemDataMigrationService.ProcessBatchesIntoAzureAsyncFromQueueJobs(blobName, stream, take);
        }
    }

    private static string ExtractBlobName(string message)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JobPayload>(message);
            if (!string.IsNullOrWhiteSpace(payload?.Input))
                return payload.Input;
        }
        catch { }

        if (Uri.TryCreate(message, UriKind.Absolute, out var uri))
            return Path.GetFileName(uri.LocalPath);

        return message;
    }

    private static int GetTakeValue(string fileName)
    {
        var marker = System.Text.RegularExpressions.Regex.Match(fileName, @"_(\d+)(?=\.xlsx?$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return marker.Success && int.TryParse(marker.Groups[1].Value, out var take) ? take : 1;
    }
}
