namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndEvidenceReportOptions
{
    public const string SectionName = "AzureRuntime:EndToEndEvidenceReport";

    public bool Enabled { get; set; } = true;

    public bool RequireDryRunResult { get; set; } = true;

    public bool IncludeDryRunStepEvidence { get; set; } = true;

    public bool TreatMissingEvidenceAsFailure { get; set; } = true;
}
