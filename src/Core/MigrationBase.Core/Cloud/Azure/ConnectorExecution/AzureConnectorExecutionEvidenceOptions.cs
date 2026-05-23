namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionEvidenceOptions
{
    public const string SectionName = "AzureRuntime:ConnectorExecutionEvidence";

    public bool Enabled { get; set; } = true;

    public bool RequireAuditHandoff { get; set; } = true;

    public bool RecordSuccessfulExecutions { get; set; } = true;

    public bool RecordSkippedExecutions { get; set; } = true;

    public bool RecordFailedExecutions { get; set; } = true;

    public bool UseInMemoryEvidenceSink { get; set; } = true;
}
