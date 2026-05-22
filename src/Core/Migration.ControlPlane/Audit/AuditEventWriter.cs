namespace Migration.ControlPlane.Audit;

public sealed class AuditEventWriter : IAuditEventWriter
{
    private readonly IAuditPersistenceProvider _provider;

    public AuditEventWriter(IAuditPersistenceProvider provider)
    {
        _provider = provider;
    }

    public Task<AuditWriteResult> WriteAsync(
        AuditEventWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var record = AuditRecordFactory.Create(
            workspaceId: request.WorkspaceId,
            category: request.Category,
            eventName: request.EventName,
            severity: request.Severity,
            tenantId: request.TenantId,
            projectId: request.ProjectId,
            runId: request.RunId,
            correlationId: request.CorrelationId,
            actor: request.Actor,
            properties: request.Properties);

        return _provider.WriteAsync(record, cancellationToken);
    }
}
