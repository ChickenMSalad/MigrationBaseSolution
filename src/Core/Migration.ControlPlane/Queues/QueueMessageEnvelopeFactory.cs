namespace Migration.ControlPlane.Queues;

public static class QueueMessageEnvelopeFactory
{
    public static QueueMessageEnvelope CreateMigrationRunEnvelope(
        string workspaceId,
        string projectId,
        string runId,
        string messageType,
        string? tenantId = null)
    {
        var createdUtc = DateTimeOffset.UtcNow;

        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [QueueMessagePropertyNames.WorkspaceId] = workspaceId,
            [QueueMessagePropertyNames.ProjectId] = projectId,
            [QueueMessagePropertyNames.RunId] = runId,
            [QueueMessagePropertyNames.CreatedUtc] = createdUtc.ToString("O"),
            [QueueMessagePropertyNames.Attempt] = "0"
        };

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            properties[QueueMessagePropertyNames.TenantId] = tenantId;
        }

        var idempotencyKey = $"{workspaceId}:{projectId}:{runId}:{messageType}";

        properties[QueueMessagePropertyNames.IdempotencyKey] = idempotencyKey;

        return new QueueMessageEnvelope(
            MessageId: Guid.NewGuid().ToString("N"),
            MessageType: messageType,
            WorkspaceId: workspaceId,
            TenantId: tenantId,
            ProjectId: projectId,
            RunId: runId,
            IdempotencyKey: idempotencyKey,
            CreatedUtc: createdUtc,
            Properties: properties);
    }
}
