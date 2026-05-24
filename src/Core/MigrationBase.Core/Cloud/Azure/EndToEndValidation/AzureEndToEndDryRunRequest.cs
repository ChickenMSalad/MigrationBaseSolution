namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndDryRunRequest
{
    public required AzureEndToEndValidationScenario Scenario { get; init; }

    public string RunId { get; init; } = "dry-run";

    public string ManifestId { get; init; } = "dry-run-manifest";

    public int SyntheticItemCount { get; init; } = 1;

    public bool IncludeFailureRuntimeProbe { get; init; }
}
