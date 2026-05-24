namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndEvidenceReportRequest
{
    public required AzureEndToEndValidationResult ValidationResult { get; init; }

    public AzureEndToEndDryRunResult? DryRunResult { get; init; }

    public bool RequireDryRunResult { get; init; } = true;
}
