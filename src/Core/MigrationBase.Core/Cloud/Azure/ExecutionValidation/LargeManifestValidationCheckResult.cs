namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public sealed record LargeManifestValidationCheckResult
{
    public string CheckName { get; init; } = string.Empty;

    public bool Passed { get; init; }

    public bool BlocksProductionReadiness { get; init; } = true;

    public string EvidenceKey { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;
}
