namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionCloseoutOptions
{
    public const string SectionName = "AzureRuntime:ConnectorExecutionCloseout";

    public bool Enabled { get; set; } = true;

    public bool RequireReadinessEvaluation { get; set; } = true;

    public bool RequireEvidenceCapture { get; set; } = true;

    public bool RequireManifestAdapter { get; set; } = true;
}
