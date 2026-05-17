namespace Migration.ControlPlane.Audit;

public sealed record AuditRecord(
    string AuditId,
    string WorkspaceId,
    string? TenantId,
    string Category,
    string EventName,
    string Severity,
    string CorrelationId,
    string? ProjectId,
    string? RunId,
    string Actor,
    DateTimeOffset CreatedUtc,
    IReadOnlyDictionary<string, string> Properties);

public sealed record AuditPersistenceProviderDescriptor(
    string ProviderKind,
    bool IsConfigured,
    bool IsDurable,
    bool SupportsQuery,
    bool SupportsArtifactLinking,
    IReadOnlyList<string> Warnings);

public sealed record AuditWriteResult(
    bool Accepted,
    string ProviderKind,
    string AuditId,
    DateTimeOffset WrittenUtc,
    string? ArtifactObjectKey = null);
