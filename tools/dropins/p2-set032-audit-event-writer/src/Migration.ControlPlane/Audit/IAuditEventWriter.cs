namespace Migration.ControlPlane.Audit;

public interface IAuditEventWriter
{
    Task<AuditWriteResult> WriteAsync(
        AuditEventWriteRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AuditEventWriteRequest(
    string WorkspaceId,
    string Category,
    string EventName,
    string Severity = "information",
    string? TenantId = null,
    string? ProjectId = null,
    string? RunId = null,
    string? CorrelationId = null,
    string? Actor = null,
    IReadOnlyDictionary<string, string>? Properties = null);
