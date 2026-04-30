using System.Text.Json;
using Azure.Storage.Queues;
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

    public AzureQueueMigrationRunQueue(IOptions<MigrationRunQueueOptions> options, ILogger<AzureQueueMigrationRunQueue> logger)
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

        var client = new QueueClient(_options.ConnectionString, _options.QueueName);
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
        await client.SendMessageAsync(json, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Enqueued migration run {RunId} to queue {QueueName}. Payload: {Payload}",
            run.RunId,
            _options.QueueName,
            json);
        //_logger.LogInformation("Queued migration run {RunId} on Azure Storage Queue {QueueName}.", run.RunId, _options.QueueName);
    }
}
