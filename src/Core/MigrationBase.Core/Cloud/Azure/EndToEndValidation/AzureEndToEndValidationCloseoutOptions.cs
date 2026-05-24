namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationCloseoutOptions
{
    public const string SectionName = "AzureRuntime:EndToEndValidationCloseout";

    public bool Enabled { get; set; } = true;

    public bool RequireReadinessEvaluation { get; set; } = true;

    public bool RequireDryRunHarness { get; set; } = true;

    public bool RequireEvidenceReport { get; set; } = true;
}
