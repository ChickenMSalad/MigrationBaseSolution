using System.Text;
using Azure.Storage.Queues;
using CloudNative.CloudEvents;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Migration.Hosts.Ashley.AemToAprimo.Functions.Models;
using Newtonsoft.Json;

namespace Migration.Hosts.Ashley.AemToAprimo.Functions.EventGridFunctions;

public sealed class AssetEventGridFunction(ILogger<AssetEventGridFunction> logger)
{
    private const string QueueName = "aem-jobs";

    [Function(nameof(AssetEventGridFunction))]
    public async Task Run([EventGridTrigger] CloudEvent cloudEvent)
    {
        if (!cloudEvent.Subject.Contains(".xlsx", StringComparison.OrdinalIgnoreCase))
            return;

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new InvalidOperationException("AzureWebJobsStorage is required.");
        var queueClient = new QueueClient(connectionString, QueueName);
        await queueClient.CreateIfNotExistsAsync();

        var jsonData = cloudEvent.Data?.ToString();
        if (string.IsNullOrWhiteSpace(jsonData))
            return;

        var blobData = JsonConvert.DeserializeObject<StorageBlobCreatedEventData>(jsonData);
        if (string.IsNullOrWhiteSpace(blobData?.Url))
            return;

        await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(blobData.Url)));
        logger.LogInformation("Queued blob url from event grid: {Url}", blobData.Url);
    }
}
