namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionContextOptions
{
    public const string SectionName = "AzureRuntime:ManifestExecutionContext";

    public bool Enabled { get; set; } = true;

    public bool RequireStateTransitionPolicy { get; set; } = true;

    public bool RecordCheckpoints { get; set; } = true;

    public int MaxCheckpointCount { get; set; } = 10000;
}
