namespace MigrationBase.Core.Cloud.Azure.Validation.Audit;

public sealed record RealMigrationAuditEvidenceRequirement
{
    public string EvidenceKey { get; init; } = string.Empty;
    public string EvidenceSource { get; init; } = string.Empty;
    public bool RequiredForProductionReadiness { get; init; } = true;
    public string RetentionPolicyKey { get; init; } = string.Empty;
}
