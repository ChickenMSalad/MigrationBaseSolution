namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionOptions
{
    public const string SectionName = "AzureRuntime:ManifestExecution";

    public bool Enabled { get; set; } = true;

    public AzureManifestExecutionMode DefaultMode { get; set; } = AzureManifestExecutionMode.ValidateOnly;

    public bool RequireManifestValidation { get; set; } = true;

    public bool RequireResultVerification { get; set; } = true;

    public int MaxPlanSteps { get; set; } = 16;
}
