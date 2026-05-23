namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public sealed record LargeManifestValidationCheck
{
    public string CheckName { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public LargeManifestValidationSeverity Severity { get; init; } = LargeManifestValidationSeverity.Required;

    public string ExpectedEvidenceKey { get; init; } = string.Empty;

    public bool BlocksProductionReadiness { get; init; } = true;
}
