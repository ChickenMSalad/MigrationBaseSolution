namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionCompletionOptions
{
    public const string SectionName = "AzureRuntime:ManifestExecutionCompletion";

    public bool Enabled { get; set; } = true;

    public bool RequireFinalCheckpoint { get; set; }

    public bool RequireAuditHandoff { get; set; } = true;

    public bool UseInMemoryCompletionSink { get; set; } = true;
}
