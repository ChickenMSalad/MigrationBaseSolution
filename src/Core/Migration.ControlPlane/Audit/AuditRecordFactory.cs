namespace Migration.ControlPlane.Audit;

public static class AuditRecordFactory
{
    public static AuditRecord Create(
        string workspaceId,
        string category,
        string eventName,
        string severity = "information",
        string? tenantId = null,
        string? projectId = null,
        string? runId = null,
        string? correlationId = null,
        string? actor = null,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        return new AuditRecord(
            AuditId: Guid.NewGuid().ToString("N"),
            WorkspaceId: string.IsNullOrWhiteSpace(workspaceId) ? "default" : workspaceId,
            TenantId: tenantId,
            Category: string.IsNullOrWhiteSpace(category) ? "general" : category,
            EventName: string.IsNullOrWhiteSpace(eventName) ? "unknown" : eventName,
            Severity: string.IsNullOrWhiteSpace(severity) ? "information" : severity,
            CorrelationId: string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId,
            ProjectId: projectId,
            RunId: runId,
            Actor: string.IsNullOrWhiteSpace(actor) ? "system" : actor,
            CreatedUtc: DateTimeOffset.UtcNow,
            Properties: properties ?? new Dictionary<string, string>());
    }
}
