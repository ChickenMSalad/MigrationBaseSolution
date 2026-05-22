using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Orchestration.Abstractions;
using Migration.Orchestration.Options;

namespace Migration.Orchestration.Progress;

public sealed class AzureQueueMigrationProgressSink : IMigrationProgressSink
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly AzureQueueMigrationProgressOptions _options;
    private readonly ILogger<AzureQueueMigrationProgressSink> _logger;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private QueueClient? _queueClient;
    private bool _initialized;

    public AzureQueueMigrationProgressSink(
        IOptions<MigrationExecutionOptions> options,
        ILogger<AzureQueueMigrationProgressSink> logger)
    {
        _options = options.Value.AzureQueueProgress;
        _logger = logger;
    }

    public async Task ReportAsync(MigrationProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        var client = await GetQueueClientAsync(cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return;
        }

        var message = CreateMessage(progressEvent);
        var json = JsonSerializer.Serialize(message, JsonOptions);

        if (Encoding.UTF8.GetByteCount(json) > _options.MaxMessageBytes)
        {
            message = message with
            {
                Message = TrimTo(progressEvent.Message, 2048),
                Properties = new Dictionary<string, string?>
                {
                    ["_truncated"] = "true",
                    ["_reason"] = "Progress event exceeded configured Azure Queue message size."
                }
            };

            json = JsonSerializer.Serialize(message, JsonOptions);
        }

        await client.SendMessageAsync(json, cancellationToken).ConfigureAwait(false);
    }

    private async Task<QueueClient?> GetQueueClientAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return _queueClient;
        }

        await _initializeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return _queueClient;
            }

            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                _logger.LogWarning(
                    "Azure Queue progress sink is configured, but MigrationExecution:AzureQueueProgress:ConnectionString is empty. Progress event publishing will be skipped.");
                _initialized = true;
                return null;
            }

            if (string.IsNullOrWhiteSpace(_options.QueueName))
            {
                _logger.LogWarning(
                    "Azure Queue progress sink is configured, but MigrationExecution:AzureQueueProgress:QueueName is empty. Progress event publishing will be skipped.");
                _initialized = true;
                return null;
            }

            var queueOptions = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };

            _queueClient = new QueueClient(_options.ConnectionString, _options.QueueName, queueOptions);

            if (_options.CreateQueueIfMissing)
            {
                await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            _initialized = true;
            return _queueClient;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    private static MigrationProgressQueueMessage CreateMessage(MigrationProgressEvent progressEvent)
    {
        return new MigrationProgressQueueMessage(
            SchemaVersion: 1,
            RunId: progressEvent.RunId,
            JobName: progressEvent.JobName,
            EventName: progressEvent.EventName,
            WorkItemId: progressEvent.WorkItemId,
            Completed: progressEvent.Completed,
            Total: progressEvent.Total,
            Message: progressEvent.Message,
            TimestampUtc: progressEvent.TimestampUtc,
            Properties: progressEvent.Properties ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));
    }

    private static string? TrimTo(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }
}

public sealed record MigrationProgressQueueMessage(
    int SchemaVersion,
    string RunId,
    string JobName,
    string EventName,
    string? WorkItemId,
    int? Completed,
    int? Total,
    string? Message,
    DateTimeOffset TimestampUtc,
    IReadOnlyDictionary<string, string?> Properties);
