using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Migration.ControlPlane.Queues;

namespace Migration.Workers.QueueExecutor;

public sealed class QueueWorkerLoopService : BackgroundService
{
    private readonly IQueueReceiveProvider _receiveProvider;
    private readonly QueueWorkerLoopOptions _options;
    private readonly ILogger<QueueWorkerLoopService> _logger;

    public QueueWorkerLoopService(
        IQueueReceiveProvider receiveProvider,
        IConfiguration configuration,
        ILogger<QueueWorkerLoopService> logger)
    {
        _receiveProvider = receiveProvider;
        _options = QueueWorkerLoopPlanner.BuildOptions(configuration);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var descriptor = QueueWorkerLoopPlanner.BuildDescriptor(_options, _receiveProvider.Descriptor);

        _logger.LogInformation(
            "Queue worker loop descriptor: enabled={Enabled}, dryRun={DryRun}, provider={Provider}, queue={Queue}, configured={Configured}",
            descriptor.Enabled,
            descriptor.DryRun,
            descriptor.ReceiveProviderKind,
            descriptor.LogicalQueueName,
            descriptor.ReceiveProviderConfigured);

        if (!_options.Enabled)
        {
            _logger.LogInformation("Queue worker loop is disabled.");
            return;
        }

        if (!_receiveProvider.Descriptor.IsConfigured)
        {
            _logger.LogWarning("Queue worker loop cannot start because receive provider is not configured.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _receiveProvider.ReceiveAsync(
                    _options.MaxMessages,
                    TimeSpan.FromSeconds(_options.VisibilityTimeoutSeconds),
                    stoppingToken).ConfigureAwait(false);

                foreach (var message in messages)
                {
                    _logger.LogInformation(
                        "Received queue message {MessageId} type {MessageType} run {RunId} idempotency {IdempotencyKey}",
                        message.ProviderMessageId,
                        message.Envelope.MessageType,
                        message.Envelope.RunId,
                        message.Envelope.IdempotencyKey);

                    if (_options.DryRun)
                    {
                        _logger.LogInformation("Dry-run enabled. Message {MessageId} will not execute.", message.ProviderMessageId);
                    }

                    if (_options.CompleteMessages)
                    {
                        await _receiveProvider.CompleteAsync(message, stoppingToken).ConfigureAwait(false);
                        _logger.LogInformation("Completed queue message {MessageId}.", message.ProviderMessageId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queue worker loop iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }
}
