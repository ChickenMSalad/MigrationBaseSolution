namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionRunnerOptions
{
    public const string SectionName = "AzureRuntime:ManifestExecutionRunner";

    public bool Enabled { get; set; } = true;

    public bool ContinueOnItemFailure { get; set; } = true;

    public int MaxItemAttempts { get; set; } = 3;

    public bool UseNoOpItemHandler { get; set; } = true;
}
