using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Migration.Hosts.WebDamToBynder.Functions.TimerFunctions;

public sealed class TenMinuteUpdatesFunction(BlobServiceClient blobServiceClient, ILogger<TenMinuteUpdatesFunction> logger)
{
    [Function("TenMinuteUpdates")]
    public async Task Run([TimerTrigger("*/10 * * * *")] TimerInfo timer)
    {
        var logContainer = blobServiceClient.GetBlobContainerClient("logs");
        var queueContainer = blobServiceClient.GetBlobContainerClient("queuejobs");

        await foreach (var blobItem in logContainer.GetBlobsAsync(prefix: "automatedupdateruns/"))
        {
            var source = logContainer.GetBlobClient(blobItem.Name);
            var target = queueContainer.GetBlobClient(Path.GetFileName(blobItem.Name));
            await target.StartCopyFromUriAsync(source.Uri);
            logger.LogInformation("Copied {Source} to queuejobs/{Target}", blobItem.Name, target.Name);
            break;
        }
    }
}
