using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Sources.Aem.Services;
using Migration.Connectors.Targets.Aprimo.Services;

namespace Migration.Hosts.Ashley.AemToAprimo.Functions.BlobFunctions;

public sealed class AssetBlobTriggerFunction(
    BlobServiceClient blobServiceClient,
    AemDataMigrationService aemDataMigrationService,
    AprimoDataMigrationService aprimoDataMigrationService,
    ILogger<AssetBlobTriggerFunction> logger)
{
    [Function(nameof(AssetBlobTriggerFunction))]
    public async Task Run([BlobTrigger("queuejobs/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name, FunctionContext context)
    {
        if (name.Contains(".processed", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Skipping already-processed blob {Name}", name);
            return;
        }

        if (name.StartsWith("getrelateddata_", StringComparison.OrdinalIgnoreCase))
        {
            await aemDataMigrationService.GetAllRelatedDataJson();
            return;
        }

        if (name.Contains("aprimoimport_", StringComparison.OrdinalIgnoreCase))
        {
            await aprimoDataMigrationService.ProcessAemAssetsFromStream(name, stream, context.CancellationToken);
            await RenameAsProcessedAsync(blobServiceClient, "queuejobs", name);
            return;
        }

        var take = GetTakeValue(name);
        await aemDataMigrationService.ProcessBatchesIntoAzureAsyncFromQueueJobs(name, stream, take);
        await RenameAsProcessedAsync(blobServiceClient, "queuejobs", name);
    }

    private static async Task RenameAsProcessedAsync(BlobServiceClient blobServiceClient, string containerName, string name)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var source = container.GetBlobClient(name);
        var target = container.GetBlobClient(name + ".processed");
        await target.StartCopyFromUriAsync(source.Uri);
        await source.DeleteIfExistsAsync();
    }

    private static int GetTakeValue(string fileName)
    {
        var marker = System.Text.RegularExpressions.Regex.Match(fileName, @"_(\d+)(?=\.xlsx?$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return marker.Success && int.TryParse(marker.Groups[1].Value, out var take) ? take : 1;
    }
}
