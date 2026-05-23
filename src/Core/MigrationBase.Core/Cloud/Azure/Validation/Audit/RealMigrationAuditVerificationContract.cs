namespace MigrationBase.Core.Cloud.Azure.Validation.Audit;

public sealed record RealMigrationAuditVerificationContract
{
    public string ContractName { get; init; } = string.Empty;
    public string ValidationProfile { get; init; } = string.Empty;
    public string SourceSystem { get; init; } = string.Empty;
    public string TargetSystem { get; init; } = string.Empty;
    public IReadOnlyList<RealMigrationAuditVerificationCheckpoint> Checkpoints { get; init; } = [];
    public IReadOnlyList<RealMigrationAuditEvidenceRequirement> EvidenceRequirements { get; init; } = [];
}
