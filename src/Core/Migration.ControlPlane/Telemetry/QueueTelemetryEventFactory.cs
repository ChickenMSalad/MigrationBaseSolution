using Migration.ControlPlane.Queues;

namespace Migration.ControlPlane.Telemetry;

public static class QueueTelemetryEventFactory
{
    public static TelemetryEventWriteRequest DispatchAccepted(
        QueueMessageEnvelope envelope,
        string providerKind,
        string queueName)
    {
        return Create(
            envelope,
            QueueTelemetryEventNames.DispatchAccepted,
            new Dictionary<string, string>
            {
                ["providerKind"] = providerKind,
                ["queueName"] = queueName,
                ["messageType"] = envelope.MessageType
            },
            new Dictionary<string, double>
            {
                ["queue.dispatch.accepted"] = 1
            });
    }

    public static TelemetryEventWriteRequest MessagePlanned(
        QueueMessageEnvelope envelope,
        QueueExecutionPlan plan)
    {
        return Create(
            envelope,
            QueueTelemetryEventNames.MessagePlanned,
            new Dictionary<string, string>
            {
                ["action"] = plan.Action,
                ["canExecute"] = plan.CanExecute.ToString()
            },
            new Dictionary<string, double>
            {
                ["queue.message.planned"] = 1,
                ["queue.message.warning_count"] = plan.Warnings.Count
            });
    }

    public static TelemetryEventWriteRequest MessageFailed(
        QueueMessageEnvelope envelope,
        string reason,
        int attempt)
    {
        return Create(
            envelope,
            QueueTelemetryEventNames.MessageFailed,
            new Dictionary<string, string>
            {
                ["reason"] = reason,
                ["attempt"] = attempt.ToString()
            },
            new Dictionary<string, double>
            {
                ["queue.message.failed"] = 1,
                ["queue.message.attempt"] = attempt
            },
            severity: "warning");
    }

    public static TelemetryEventWriteRequest CoordinatorPolled(
        string workspaceId,
        QueueExecutorCoordinatorResult result)
    {
        return new TelemetryEventWriteRequest(
            WorkspaceId: workspaceId,
            EventName: QueueTelemetryEventNames.CoordinatorPolled,
            Category: TelemetryCategories.Queue,
            Severity: result.FailureCount > 0 ? "warning" : "information",
            Dimensions: new Dictionary<string, string>
            {
                ["hasFailures"] = (result.FailureCount > 0).ToString()
            },
            Metrics: new Dictionary<string, double>
            {
                ["queue.coordinator.received_count"] = result.ReceivedCount,
                ["queue.coordinator.planned_count"] = result.PlannedCount,
                ["queue.coordinator.executable_count"] = result.ExecutableCount,
                ["queue.coordinator.completed_count"] = result.CompletedCount,
                ["queue.coordinator.failure_count"] = result.FailureCount
            });
    }

    private static TelemetryEventWriteRequest Create(
        QueueMessageEnvelope envelope,
        string eventName,
        IReadOnlyDictionary<string, string> dimensions,
        IReadOnlyDictionary<string, double> metrics,
        string severity = "information")
    {
        return new TelemetryEventWriteRequest(
            WorkspaceId: envelope.WorkspaceId,
            EventName: eventName,
            Category: TelemetryCategories.Queue,
            Severity: severity,
            TenantId: envelope.TenantId,
            ProjectId: envelope.ProjectId,
            RunId: envelope.RunId,
            CorrelationId: envelope.IdempotencyKey,
            Dimensions: dimensions,
            Metrics: metrics);
    }
}
