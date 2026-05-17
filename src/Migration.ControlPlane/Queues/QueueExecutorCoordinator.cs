namespace Migration.ControlPlane.Queues;

public sealed class QueueExecutorCoordinator : IQueueExecutorCoordinator
{
    private readonly IQueueReceiveProvider _receiveProvider;
    private readonly IQueueExecutionPlanner _executionPlanner;
    private readonly IQueueFailureHandler _failureHandler;
    private readonly QueuePoisonHandlingPlan _poisonPlan;

    public QueueExecutorCoordinator(
        IQueueReceiveProvider receiveProvider,
        IQueueExecutionPlanner executionPlanner,
        IQueueFailureHandler failureHandler,
        QueuePoisonHandlingPlan poisonPlan)
    {
        _receiveProvider = receiveProvider;
        _executionPlanner = executionPlanner;
        _failureHandler = failureHandler;
        _poisonPlan = poisonPlan;
    }

    public async Task<QueueExecutorCoordinatorResult> PollOnceAsync(
        QueueExecutorCoordinatorOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var warnings = new List<string>();

        if (!_receiveProvider.Descriptor.IsConfigured)
        {
            warnings.Add("Queue receive provider is not configured.");
            return new QueueExecutorCoordinatorResult(
                ReceivedCount: 0,
                PlannedCount: 0,
                ExecutableCount: 0,
                CompletedCount: 0,
                FailureCount: 0,
                Messages: Array.Empty<QueueExecutorMessageResult>(),
                Warnings: warnings);
        }

        if (options.DryRun)
        {
            warnings.Add("Coordinator is running in dry-run mode. No migration execution will be performed.");
        }

        var received = await _receiveProvider.ReceiveAsync(
            maxMessages: Math.Max(1, options.MaxMessages),
            visibilityTimeout: TimeSpan.FromMinutes(5),
            cancellationToken).ConfigureAwait(false);

        var results = new List<QueueExecutorMessageResult>();
        var completed = 0;
        var failures = 0;

        foreach (var message in received)
        {
            var plan = _executionPlanner.Plan(message.Envelope);
            var messageWarnings = plan.Warnings.ToList();
            var failureHandled = false;
            string? failureArtifactObjectKey = null;

            if (!plan.CanExecute)
            {
                failures++;

                if (options.WriteFailureArtifacts)
                {
                    var failureRequest = BuildFailureRequest(message, plan);
                    var failureResult = await _failureHandler.HandleFailureAsync(
                        failureRequest,
                        _poisonPlan,
                        cancellationToken).ConfigureAwait(false);

                    failureHandled = failureResult.FailureArtifactWritten;
                    failureArtifactObjectKey = failureResult.ArtifactObjectKey;

                    foreach (var warning in failureResult.Warnings)
                    {
                        messageWarnings.Add(warning);
                    }
                }
            }

            if (plan.CanExecute && !options.DryRun && options.CompleteMessages)
            {
                await _receiveProvider.CompleteAsync(message, cancellationToken).ConfigureAwait(false);
                completed++;
            }

            results.Add(new QueueExecutorMessageResult(
                ProviderMessageId: message.ProviderMessageId,
                MessageType: message.Envelope.MessageType,
                ProjectId: message.Envelope.ProjectId,
                RunId: message.Envelope.RunId,
                IdempotencyKey: message.Envelope.IdempotencyKey,
                CanExecute: plan.CanExecute,
                Action: plan.Action,
                Completed: plan.CanExecute && !options.DryRun && options.CompleteMessages,
                FailureHandled: failureHandled,
                FailureArtifactObjectKey: failureArtifactObjectKey,
                Warnings: messageWarnings));
        }

        return new QueueExecutorCoordinatorResult(
            ReceivedCount: received.Count,
            PlannedCount: results.Count,
            ExecutableCount: results.Count(x => x.CanExecute),
            CompletedCount: completed,
            FailureCount: failures,
            Messages: results,
            Warnings: warnings);
    }

    private static QueueFailureArtifactRequest BuildFailureRequest(
        QueueReceivedMessage message,
        QueueExecutionPlan plan)
    {
        return new QueueFailureArtifactRequest(
            WorkspaceId: message.Envelope.WorkspaceId,
            ProjectId: message.Envelope.ProjectId ?? "unknown-project",
            RunId: message.Envelope.RunId ?? "unknown-run",
            MessageType: message.Envelope.MessageType,
            IdempotencyKey: message.Envelope.IdempotencyKey,
            FailureReason: "queue-execution-plan-invalid",
            ExceptionType: "QueueExecutionPlanValidation",
            ExceptionMessage: string.Join("; ", plan.Warnings),
            Attempt: message.DequeueCount ?? 1,
            FailedUtc: DateTimeOffset.UtcNow);
    }
}
