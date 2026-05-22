namespace Migration.ControlPlane.Queues;

public sealed class QueueExecutionPlanner : IQueueExecutionPlanner
{
    public QueueExecutionPlan Plan(QueueMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var warnings = new List<string>();

        var requiresRunId = envelope.MessageType is
            QueueMessageTypes.MigrationRunExecute or
            QueueMessageTypes.MigrationRunCancel or
            QueueMessageTypes.MigrationRunResume;

        var requiresProjectId = envelope.MessageType is
            QueueMessageTypes.MigrationRunExecute or
            QueueMessageTypes.MigrationRunResume;

        if (requiresRunId && string.IsNullOrWhiteSpace(envelope.RunId))
        {
            warnings.Add("Message requires runId but runId is missing.");
        }

        if (requiresProjectId && string.IsNullOrWhiteSpace(envelope.ProjectId))
        {
            warnings.Add("Message requires projectId but projectId is missing.");
        }

        if (string.IsNullOrWhiteSpace(envelope.WorkspaceId))
        {
            warnings.Add("Message workspaceId is missing.");
        }

        if (string.IsNullOrWhiteSpace(envelope.IdempotencyKey))
        {
            warnings.Add("Message idempotencyKey is missing.");
        }

        var action = envelope.MessageType switch
        {
            QueueMessageTypes.MigrationRunExecute => "execute-run",
            QueueMessageTypes.MigrationRunCancel => "cancel-run",
            QueueMessageTypes.MigrationRunResume => "resume-run",
            _ => "unknown"
        };

        if (action == "unknown")
        {
            warnings.Add($"Message type '{envelope.MessageType}' is not recognized.");
        }

        return new QueueExecutionPlan(
            MessageType: envelope.MessageType,
            WorkspaceId: envelope.WorkspaceId,
            TenantId: envelope.TenantId,
            ProjectId: envelope.ProjectId,
            RunId: envelope.RunId,
            IdempotencyKey: envelope.IdempotencyKey,
            Action: action,
            CanExecute: warnings.Count == 0 && action != "unknown",
            RequiresRunId: requiresRunId,
            RequiresProjectId: requiresProjectId,
            Warnings: warnings);
    }
}
