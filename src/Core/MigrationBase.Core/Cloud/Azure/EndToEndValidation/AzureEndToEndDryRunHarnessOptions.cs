namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndDryRunHarnessOptions
{
    public const string SectionName = "AzureRuntime:EndToEndDryRunHarness";

    public bool Enabled { get; set; } = true;

    public int SyntheticItemCount { get; set; } = 1;

    public bool IncludeFailureRuntimeProbe { get; set; } = true;

    public bool RequireAllStepsPassed { get; set; } = true;
}
