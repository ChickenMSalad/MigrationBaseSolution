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
    private readonly MigrationRunQueueOptions _queueOptions;
    private readonly QueueExecutorOptions _executorOptions;
    private readonly ILogger<MigrationRunQueueWorker> _logger;

    public MigrationRunQueueWorker(
        IAdminProjectStore store,
        IMigrationJobRunner runner,
        IOptions<MigrationRunQueueOptions> queueOptions,
        IOptions<QueueExecutorOptions> executorOptions,
        ILogger<MigrationRunQueueWorker> logger)
    {
        _store = store;
        _runner = runner;
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

        var queue = new QueueClient(
            _queueOptions.ConnectionString,
            _queueOptions.QueueName,
            new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });

        if (_queueOptions.CreateIfMissing)
        {
            await queue.CreateIfNotExistsAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Migration queue executor started. Queue={QueueName}; ExecuteRuns={ExecuteRuns}",
            _queueOptions.QueueName,
            _executorOptions.ExecuteRuns);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Polling migration queue {QueueName}", _queueOptions.QueueName);

                var response = await queue.ReceiveMessagesAsync(
                    maxMessages: Math.Max(1, _executorOptions.MaxMessagesPerPoll),
                    visibilityTimeout: TimeSpan.FromMinutes(Math.Max(1, _executorOptions.VisibilityTimeoutMinutes)),
                    cancellationToken: stoppingToken).ConfigureAwait(false);

                _logger.LogInformation("Queue poll returned {Count} messages", response.Value.Length);

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
                        var body = message.Body.ToString();
                        var runMessage = JsonSerializer.Deserialize<QueuedMigrationRunMessage>(body, JsonOptions);

                        if (runMessage is null || string.IsNullOrWhiteSpace(runMessage.RunId))
                        {
                            _logger.LogWarning("Discarding malformed migration queue message {MessageId}.", message.MessageId);
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
            _logger.LogInformation("Executing migration run {RunId} ({JobName}).", running.RunId, running.JobName);

            var summary = await _runner.RunAsync(running.Job, cancellationToken).ConfigureAwait(false);

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
