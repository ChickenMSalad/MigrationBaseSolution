namespace MigrationBase.Core.Cloud.Azure.Validation.Audit;

public sealed record RealMigrationAuditVerificationCheckpoint
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public RealMigrationAuditVerificationLevel Level { get; init; } = RealMigrationAuditVerificationLevel.Required;
    public string ExpectedEvidenceType { get; init; } = string.Empty;
    public bool BlocksPromotion { get; init; } = true;
}
