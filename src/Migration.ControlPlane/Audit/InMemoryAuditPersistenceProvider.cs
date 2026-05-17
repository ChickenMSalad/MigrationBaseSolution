namespace Migration.ControlPlane.Audit;

public sealed class InMemoryAuditPersistenceProvider : IAuditPersistenceProvider
{
    private readonly object _gate = new();
    private readonly List<AuditRecord> _records = [];

    public AuditPersistenceProviderDescriptor Descriptor { get; } = new(
        ProviderKind: "inMemory",
        IsConfigured: true,
        IsDurable: false,
        SupportsQuery: true,
        SupportsArtifactLinking: false,
        Warnings:
        [
            "In-memory audit persistence is diagnostics-only and does not survive process restarts."
        ]);

    public Task<AuditWriteResult> WriteAsync(
        AuditRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_gate)
        {
            _records.Add(record);
        }

        return Task.FromResult(new AuditWriteResult(
            Accepted: true,
            ProviderKind: Descriptor.ProviderKind,
            AuditId: record.AuditId,
            WrittenUtc: DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<AuditRecord>> QueryRecentAsync(
        string workspaceId,
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var records = _records
                .Where(x => string.Equals(x.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CreatedUtc)
                .Take(Math.Clamp(take, 1, 250))
                .ToArray();

            return Task.FromResult<IReadOnlyList<AuditRecord>>(records);
        }
    }
}
