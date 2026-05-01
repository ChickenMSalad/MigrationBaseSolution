using System.Text.Json;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.ControlPlane.Models;
using Migration.ControlPlane.Options;

namespace Migration.ControlPlane.Queues;

public sealed class AzureQueueMigrationRunQueue : IMigrationRunQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly MigrationRunQueueOptions _options;
    private readonly ILogger<AzureQueueMigrationRunQueue> _logger;

    public AzureQueueMigrationRunQueue(
        IOptions<MigrationRunQueueOptions> options,
        ILogger<AzureQueueMigrationRunQueue> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnqueueAsync(MigrationRunControlRecord run, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("MigrationRunQueue:ConnectionString is required when Provider is AzureQueue.");
        }

        if (string.IsNullOrWhiteSpace(_options.QueueName))
        {
            throw new InvalidOperationException("MigrationRunQueue:QueueName is required when Provider is AzureQueue.");
        }

        var client = CreateQueueClient();

        if (_options.CreateIfMissing)
        {
            await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var message = new QueuedMigrationRunMessage
        {
            RunId = run.RunId,
            ProjectId = run.ProjectId,
            JobName = run.JobName,
            PreflightOnly = string.Equals(run.Status, AdminRunStatuses.PreflightQueued, StringComparison.OrdinalIgnoreCase)
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        try
        {
            _logger.LogInformation(
                "Sending migration run queue message. Queue={QueueName}; RunId={RunId}; JsonLength={JsonLength}; Base64Encoding=true; ServiceVersion=V2021_12_02",
                _options.QueueName,
                run.RunId,
                json.Length);

            await client.SendMessageAsync(json, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Enqueued migration run {RunId} to Azure Storage Queue {QueueName}.",
                run.RunId,
                _options.QueueName);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Azure Storage Queue enqueue failed. Queue={QueueName}; Status={Status}; ErrorCode={ErrorCode}; Message={Message}; JsonLength={JsonLength}",
                _options.QueueName,
                ex.Status,
                ex.ErrorCode,
                ex.Message,
                json.Length);

            throw;
        }
    }

    private QueueClient CreateQueueClient()
    {
        var options = new QueueClientOptions(QueueClientOptions.ServiceVersion.V2021_12_02)
        {
            MessageEncoding = QueueMessageEncoding.Base64
        };

        return new QueueClient(_options.ConnectionString, _options.QueueName, options);
    }
}
