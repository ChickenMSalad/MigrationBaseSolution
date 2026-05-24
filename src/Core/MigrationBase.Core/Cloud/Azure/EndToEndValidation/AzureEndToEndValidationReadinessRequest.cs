namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationReadinessRequest
{
    public required string RuntimeName { get; init; }

    public bool RequireValidationRunner { get; init; } = true;

    public bool RequireDryRunHarness { get; init; } = true;

    public bool RequireEvidenceReportBuilder { get; init; } = true;
}
