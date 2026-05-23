namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureRuntimeOptions
{
    public const string SectionName = "AzureRuntime:FailureRuntime";

    public bool Enabled { get; set; } = true;

    public bool ClassifyTransientFailures { get; set; } = true;

    public bool ClassifyPoisonFailures { get; set; } = true;

    public bool AllowReplayEligibilityClassification { get; set; } = true;
}
