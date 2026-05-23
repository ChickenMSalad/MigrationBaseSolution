namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionCloseoutOptions
{
    public const string SectionName = "AzureRuntime:ManifestExecutionCloseout";

    public bool Enabled { get; set; } = true;

    public bool RequireReadinessEvaluation { get; set; } = true;

    public bool RequireCompletionHandoff { get; set; } = true;

    public bool RequireCheckpointBoundary { get; set; } = true;
}
