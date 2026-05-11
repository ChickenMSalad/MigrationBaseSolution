using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Functions.TimerFunctions;

public sealed class HourlyLogCheckerFunction(BlobServiceClient blobServiceClient, ILogger<HourlyLogCheckerFunction> logger)
{
    [Function("HourlyLogChecker")]
    public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo timer)
    {
        var logContainer = blobServiceClient.GetBlobContainerClient("logs");
        var queueContainer = blobServiceClient.GetBlobContainerClient("queuejobs");

        var logKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await foreach (var blob in logContainer.GetBlobsAsync(prefix: "logfolder/"))
        {
            logKeys.Add(Path.GetFileNameWithoutExtension(blob.Name));
        }

        await foreach (var blob in logContainer.GetBlobsAsync(prefix: "automatedruns/"))
        {
            var key = Path.GetFileNameWithoutExtension(blob.Name);
            if (logKeys.Contains(key))
                continue;

            var source = logContainer.GetBlobClient(blob.Name);
            var target = queueContainer.GetBlobClient(Path.GetFileName(blob.Name));
            await target.StartCopyFromUriAsync(source.Uri);
            logger.LogInformation("Queued unmatched automated run {BlobName}", blob.Name);
            break;
        }
    }
}
