using Microsoft.Extensions.Configuration;

namespace Migration.ControlPlane.Queues;

public sealed class QueueExecutionObservabilityService : IQueueExecutionObservabilityService
{
    private readonly IConfiguration _configuration;
    private readonly IQueueReceiveProvider _receiveProvider;

    public QueueExecutionObservabilityService(
        IConfiguration configuration,
        IQueueReceiveProvider receiveProvider)
    {
        _configuration = configuration;
        _receiveProvider = receiveProvider;
    }

    public QueueExecutionObservabilitySnapshot GetSnapshot()
    {
        var loopOptions = QueueWorkerLoopPlanner.BuildOptions(_configuration);
        var coordinatorOptions =
            QueueExecutorCoordinatorRegistrationExtensions.BuildOptions(_configuration);

        var warnings = new List<string>();

        if (!loopOptions.Enabled)
        {
            warnings.Add("Queue worker loop is disabled.");
        }

        if (coordinatorOptions.DryRun)
        {
            warnings.Add("Coordinator is running in dry-run mode.");
        }

        if (!coordinatorOptions.CompleteMessages)
        {
            warnings.Add("Messages will not be completed/deleted.");
        }

        if (!_receiveProvider.Descriptor.IsConfigured)
        {
            warnings.Add("Queue receive provider is not configured.");
        }

        return new QueueExecutionObservabilitySnapshot(
            GeneratedUtc: DateTimeOffset.UtcNow,
            ProviderKind: _receiveProvider.Descriptor.ProviderKind,
            QueueName: _receiveProvider.Descriptor.LogicalQueueName,
            ReceiveProviderConfigured: _receiveProvider.Descriptor.IsConfigured,
            WorkerLoopEnabled: loopOptions.Enabled,
            WorkerLoopDryRun: loopOptions.DryRun,
            CoordinatorDryRun: coordinatorOptions.DryRun,
            CompleteMessages: coordinatorOptions.CompleteMessages,
            MaxMessages: coordinatorOptions.MaxMessages,
            SupportedMessageTypes:
            [
                QueueMessageTypes.MigrationRunExecute,
                QueueMessageTypes.MigrationRunCancel,
                QueueMessageTypes.MigrationRunResume
            ],
            Warnings: warnings);
    }
}
