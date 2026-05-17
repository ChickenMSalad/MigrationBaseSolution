using Migration.ControlPlane.Queues;

namespace Migration.ControlPlane.Audit;

public static class QueueAuditEventFactory
{
    public static AuditEventWriteRequest DispatchAccepted(
        QueueMessageEnvelope envelope,
        string providerKind,
        string queueName)
    {
        return Create(
            envelope,
            QueueAuditEventNames.DispatchAccepted,
            new Dictionary<string, string>
            {
                ["providerKind"] = providerKind,
                ["queueName"] = queueName,
                ["messageId"] = envelope.MessageId
            });
    }

    public static AuditEventWriteRequest MessagePlanned(
        QueueMessageEnvelope envelope,
        QueueExecutionPlan plan)
    {
        return Create(
            envelope,
            QueueAuditEventNames.MessagePlanned,
            new Dictionary<string, string>
            {
                ["action"] = plan.Action,
                ["canExecute"] = plan.CanExecute.ToString(),
                ["warningCount"] = plan.Warnings.Count.ToString()
            });
    }

    public static AuditEventWriteRequest MessageFailed(
        QueueMessageEnvelope envelope,
        string reason,
        int attempt)
    {
        return Create(
            envelope,
            QueueAuditEventNames.MessageFailed,
            new Dictionary<string, string>
            {
                ["reason"] = reason,
                ["attempt"] = attempt.ToString()
            },
            severity: "warning");
    }

    public static AuditEventWriteRequest FailureArtifactWritten(
        QueueFailureArtifactRequest failure,
        string artifactObjectKey)
    {
        return new AuditEventWriteRequest(
            WorkspaceId: failure.WorkspaceId,
            Category: AuditCategories.Queue,
            EventName: QueueAuditEventNames.FailureArtifactWritten,
            Severity: "warning",
            ProjectId: failure.ProjectId,
            RunId: failure.RunId,
            CorrelationId: failure.IdempotencyKey,
            Actor: "queue-worker",
            Properties: new Dictionary<string, string>
            {
                ["messageType"] = failure.MessageType,
                ["idempotencyKey"] = failure.IdempotencyKey,
                ["artifactObjectKey"] = artifactObjectKey,
                ["attempt"] = failure.Attempt.ToString()
            });
    }

    public static AuditEventWriteRequest CoordinatorPolled(
        string workspaceId,
        QueueExecutorCoordinatorResult result)
    {
        return new AuditEventWriteRequest(
            WorkspaceId: workspaceId,
            Category: AuditCategories.Queue,
            EventName: QueueAuditEventNames.CoordinatorPolled,
            Severity: result.FailureCount > 0 ? "warning" : "information",
            Actor: "queue-coordinator",
            Properties: new Dictionary<string, string>
            {
                ["receivedCount"] = result.ReceivedCount.ToString(),
                ["plannedCount"] = result.PlannedCount.ToString(),
                ["executableCount"] = result.ExecutableCount.ToString(),
                ["completedCount"] = result.CompletedCount.ToString(),
                ["failureCount"] = result.FailureCount.ToString()
            });
    }

    private static AuditEventWriteRequest Create(
        QueueMessageEnvelope envelope,
        string eventName,
        IReadOnlyDictionary<string, string> properties,
        string severity = "information")
    {
        return new AuditEventWriteRequest(
            WorkspaceId: envelope.WorkspaceId,
            Category: AuditCategories.Queue,
            EventName: eventName,
            Severity: severity,
            TenantId: envelope.TenantId,
            ProjectId: envelope.ProjectId,
            RunId: envelope.RunId,
            CorrelationId: envelope.IdempotencyKey,
            Actor: "queue-worker",
            Properties: properties);
    }
}
