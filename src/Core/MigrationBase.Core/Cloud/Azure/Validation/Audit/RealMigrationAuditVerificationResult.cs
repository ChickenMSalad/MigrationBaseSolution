namespace MigrationBase.Core.Cloud.Azure.Validation.Audit;

public sealed record RealMigrationAuditVerificationResult
{
    public string ContractName { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public IReadOnlyList<string> PassedCheckpoints { get; init; } = [];
    public IReadOnlyList<string> FailedCheckpoints { get; init; } = [];
    public IReadOnlyList<string> MissingEvidenceKeys { get; init; } = [];
}
