namespace Migration.ControlPlane.Audit;

public interface IAuditPersistenceProvider
{
    AuditPersistenceProviderDescriptor Descriptor { get; }

    Task<AuditWriteResult> WriteAsync(
        AuditRecord record,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditRecord>> QueryRecentAsync(
        string workspaceId,
        int take = 25,
        CancellationToken cancellationToken = default);
}
