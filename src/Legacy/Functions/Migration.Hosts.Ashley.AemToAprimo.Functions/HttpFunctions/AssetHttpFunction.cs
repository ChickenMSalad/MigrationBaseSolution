using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Migration.Hosts.Ashley.AemToAprimo.Functions.Models;

namespace Migration.Hosts.Ashley.AemToAprimo.Functions.HttpFunctions;

public sealed class AssetHttpFunction(ILogger<AssetHttpFunction> logger)
{
    private const string QueueName = "aem-jobs";

    [Function(nameof(AssetHttpFunction))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var filename = query["filename"];
        if (string.IsNullOrWhiteSpace(filename))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Provide ?filename=<blobname>");
            return bad;
        }

        var payload = JsonSerializer.Serialize(new JobPayload { Input = filename });
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new InvalidOperationException("AzureWebJobsStorage is required.");
        var queueClient = new QueueClient(connectionString, QueueName);
        await queueClient.CreateIfNotExistsAsync();
        await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)));

        logger.LogInformation("Queued AEM job for {Filename}", filename);
        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteStringAsync($"Queued {filename}");
        return response;
    }
}
