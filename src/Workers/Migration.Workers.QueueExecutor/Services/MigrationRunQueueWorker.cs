using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.ControlPlane.Models;
using Migration.ControlPlane.Options;
using Migration.ControlPlane.Services;
using Migration.Orchestration.Abstractions;
using Migration.Workers.QueueExecutor.Options;

namespace Migration.Workers.QueueExecutor.Services;

public sealed class MigrationRunQueueWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAdminProjectStore _store;
    private readonly IMigrationJobRunner _runner;
    private readonly ProjectCredentialJobSettingsHydrator _credentialHydrator;
    private readonly MigrationRunQueueOptions _queueOptions;
    private readonly QueueExecutorOptions _executorOptions;
    private readonly ILogger<MigrationRunQueueWorker> _logger;

    public MigrationRunQueueWorker(
        IAdminProjectStore store,
        IMigrationJobRunner runner,
        ProjectCredentialJobSettingsHydrator credentialHydrator,
        IOptions<MigrationRunQueueOptions> queueOptions,
        IOptions<QueueExecutorOptions> executorOptions,
        ILogger<MigrationRunQueueWorker> logger)
    {
        _store = store;
        _runner = runner;
        _credentialHydrator = credentialHydrator;
        _queueOptions = queueOptions.Value;
        _executorOptions = executorOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_queueOptions.Provider.Equals("AzureQueue", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Queue executor is idle because MigrationRunQueue:Provider is {Provider}. Set it to AzureQueue to process queued runs.",
                _queueOptions.Provider);
            return;
        }

        if (string.IsNullOrWhiteSpace(_queueOptions.ConnectionString))
        {
            throw new InvalidOperationException("MigrationRunQueue:ConnectionString is required when Provider is AzureQueue.");
        }

        if (string.IsNullOrWhiteSpace(_queueOptions.QueueName))
        {
            throw new InvalidOperationException("MigrationRunQueue:QueueName is required when Provider is AzureQueue.");
        }

        var queue = CreateQueueClient();
        if (_queueOptions.CreateIfMissing)
        {
            await queue.CreateIfNotExistsAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Migration queue executor started. Queue={QueueName}; ExecuteRuns={ExecuteRuns}; Base64Encoding=true; ServiceVersion=V2021_12_02",
            _queueOptions.QueueName,
            _executorOptions.ExecuteRuns);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await queue.ReceiveMessagesAsync(
                    maxMessages: Math.Max(1, _executorOptions.MaxMessagesPerPoll),
                    visibilityTimeout: TimeSpan.FromMinutes(Math.Max(1, _executorOptions.VisibilityTimeoutMinutes)),
                    cancellationToken: stoppingToken).ConfigureAwait(false);

                if (response.Value.Length == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _executorOptions.PollDelaySeconds)), stoppingToken).ConfigureAwait(false);
                    continue;
                }

                foreach (var message in response.Value)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    var deleteMessage = false;

                    try
                    {
                        if (!TryDeserializeMessage(message, out var runMessage, out var reason) || runMessage is null)
                        {
                            _logger.LogWarning(
                                "Deleting malformed migration queue message {MessageId}. Reason={Reason}; DequeueCount={DequeueCount}.",
                                message.MessageId,
                                reason,
                                message.DequeueCount);
                            deleteMessage = true;
                        }
                        else
                        {
                            deleteMessage = await ProcessRunMessageAsync(runMessage, stoppingToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed while processing queue message {MessageId}. The message will become visible again after the visibility timeout.",
                            message.MessageId);
                    }

                    if (deleteMessage)
                    {
                        await queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queue polling failed. Retrying after delay.");
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _executorOptions.PollDelaySeconds)), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private QueueClient CreateQueueClient()
    {
        var options = new QueueClientOptions(QueueClientOptions.ServiceVersion.V2021_12_02)
        {
            MessageEncoding = QueueMessageEncoding.Base64
        };

        return new QueueClient(_queueOptions.ConnectionString, _queueOptions.QueueName, options);
    }

    private static bool TryDeserializeMessage(
        QueueMessage message,
        out QueuedMigrationRunMessage? runMessage,
        out string reason)
    {
        runMessage = null;
        reason = string.Empty;

        var body = message.Body.ToString();
        if (string.IsNullOrWhiteSpace(body))
        {
            reason = "Message body was empty.";
            return false;
        }

        if (TryDeserializeJson(body, out runMessage))
        {
            return HasUsableRunId(runMessage, out reason);
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(body));
            if (string.IsNullOrWhiteSpace(decoded))
            {
                reason = "Base64-decoded message body was empty.";
                return false;
            }

            if (TryDeserializeJson(decoded, out runMessage))
            {
                return HasUsableRunId(runMessage, out reason);
            }
        }
        catch (FormatException)
        {
            // Not Base64. Fall through to final malformed result.
        }
        catch (ArgumentException)
        {
            // Defensive: malformed encoded payload. Fall through to final malformed result.
        }

        reason = "Message body was neither valid JSON nor Base64-encoded JSON.";
        return false;
    }

    private static bool TryDeserializeJson(string json, out QueuedMigrationRunMessage? runMessage)
    {
        runMessage = null;

        try
        {
            runMessage = JsonSerializer.Deserialize<QueuedMigrationRunMessage>(json, JsonOptions);
            return runMessage is not null;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static bool HasUsableRunId(QueuedMigrationRunMessage? runMessage, out string reason)
    {
        if (runMessage is null)
        {
            reason = "Message JSON deserialized to null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(runMessage.RunId))
        {
            reason = "Message JSON did not contain a runId.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private async Task<bool> ProcessRunMessageAsync(QueuedMigrationRunMessage message, CancellationToken cancellationToken)
    {
        var run = await _store.GetRunAsync(message.RunId, cancellationToken).ConfigureAwait(false);
        if (run is null)
        {
            _logger.LogWarning("Run control record {RunId} was not found.", message.RunId);
            return _executorOptions.DeleteMessageWhenRunRecordIsMissing;
        }

        if (run.Status is AdminRunStatuses.Completed or AdminRunStatuses.Failed or AdminRunStatuses.Canceled)
        {
            _logger.LogInformation("Run {RunId} is already terminal ({Status}); deleting queue message.", run.RunId, run.Status);
            return true;
        }

        if (!_executorOptions.ExecuteRuns)
        {
            _logger.LogInformation("ExecuteRuns=false; leaving run {RunId} queued without execution.", run.RunId);
            return false;
        }

        var running = run with
        {
            Status = AdminRunStatuses.Running,
            StartedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
            Message = "Run picked up by queue executor."
        };

        await _store.SaveRunAsync(running, cancellationToken).ConfigureAwait(false);

        try
        {
            var hydratedJob = await _credentialHydrator.HydrateAsync(running.Job, cancellationToken).ConfigureAwait(false);
            running = running with { Job = hydratedJob };
            await _store.SaveRunAsync(running, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Executing migration run {RunId} ({JobName}).", running.RunId, running.JobName);
            var summary = await _runner.RunAsync(hydratedJob, cancellationToken).ConfigureAwait(false);

            var completed = running with
            {
                Status = summary.Failed == 0 ? AdminRunStatuses.Completed : AdminRunStatuses.Failed,
                CompletedUtc = DateTimeOffset.UtcNow,
                UpdatedUtc = DateTimeOffset.UtcNow,
                Message = $"Worker completed. Total={summary.TotalWorkItems}; Succeeded={summary.Succeeded}; Failed={summary.Failed}; Skipped={summary.Skipped}; Elapsed={summary.Elapsed:g}."
            };

            await _store.SaveRunAsync(completed, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var canceled = running with
            {
                Status = AdminRunStatuses.Canceled,
                CompletedUtc = DateTimeOffset.UtcNow,
                UpdatedUtc = DateTimeOffset.UtcNow,
                Message = "Worker execution was canceled."
            };

            await _store.SaveRunAsync(canceled, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration run {RunId} failed.", running.RunId);

            var failed = running with
            {
                Status = AdminRunStatuses.Failed,
                CompletedUtc = DateTimeOffset.UtcNow,
                UpdatedUtc = DateTimeOffset.UtcNow,
                Message = ex.Message
            };

            await _store.SaveRunAsync(failed, CancellationToken.None).ConfigureAwait(false);
            return true;
        }
    }
}
