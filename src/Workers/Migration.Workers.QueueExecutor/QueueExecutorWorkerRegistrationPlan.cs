using Migration.ControlPlane.Queues;

namespace Migration.Workers.QueueExecutor;

public sealed record QueueExecutorWorkerRegistrationPlan(
    bool CanRegisterCoordinator,
    bool WorkerLoopEnabled,
    bool CoordinatorDryRun,
    bool CompleteMessages,
    bool WriteFailureArtifacts,
    string ReceiveProviderKind,
    string LogicalQueueName,
    bool ReceiveProviderConfigured,
    IReadOnlyList<string> RequiredServices,
    IReadOnlyList<string> Warnings);

public static class QueueExecutorWorkerRegistrationPlanBuilder
{
    public static QueueExecutorWorkerRegistrationPlan Build(
        QueueWorkerLoopOptions loopOptions,
        QueueExecutorCoordinatorOptions coordinatorOptions,
        QueueReceiveProviderDescriptor receiveProvider)
    {
        ArgumentNullException.ThrowIfNull(loopOptions);
        ArgumentNullException.ThrowIfNull(coordinatorOptions);
        ArgumentNullException.ThrowIfNull(receiveProvider);

        var warnings = new List<string>();

        if (!loopOptions.Enabled)
        {
            warnings.Add("Queue worker loop is disabled.");
        }

        if (coordinatorOptions.DryRun)
        {
            warnings.Add("Queue executor coordinator is in dry-run mode.");
        }

        if (!coordinatorOptions.CompleteMessages)
        {
            warnings.Add("Coordinator will not complete/delete messages.");
        }

        if (!receiveProvider.IsConfigured)
        {
            warnings.Add("Queue receive provider is not configured.");
        }

        return new QueueExecutorWorkerRegistrationPlan(
            CanRegisterCoordinator: true,
            WorkerLoopEnabled: loopOptions.Enabled,
            CoordinatorDryRun: coordinatorOptions.DryRun,
            CompleteMessages: coordinatorOptions.CompleteMessages,
            WriteFailureArtifacts: coordinatorOptions.WriteFailureArtifacts,
            ReceiveProviderKind: receiveProvider.ProviderKind,
            LogicalQueueName: receiveProvider.LogicalQueueName,
            ReceiveProviderConfigured: receiveProvider.IsConfigured,
            RequiredServices:
            [
                "IQueueReceiveProvider",
                "IQueueExecutionPlanner",
                "IQueueFailureHandler",
                "IQueueExecutorCoordinator"
            ],
            Warnings: warnings);
    }
}
