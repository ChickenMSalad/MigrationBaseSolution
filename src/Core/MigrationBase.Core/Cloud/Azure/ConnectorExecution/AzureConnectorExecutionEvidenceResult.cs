namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionEvidenceResult
{
    private AzureConnectorExecutionEvidenceResult(
        bool recorded,
        AzureConnectorExecutionEvidenceRecord? record,
        string? reason)
    {
        Recorded = recorded;
        Record = record;
        Reason = reason;
    }

    public bool Recorded { get; }

    public AzureConnectorExecutionEvidenceRecord? Record { get; }

    public string? Reason { get; }

    public static AzureConnectorExecutionEvidenceResult Success(
        AzureConnectorExecutionEvidenceRecord record)
    {
        return new AzureConnectorExecutionEvidenceResult(true, record, null);
    }

    public static AzureConnectorExecutionEvidenceResult Rejected(string reason)
    {
        return new AzureConnectorExecutionEvidenceResult(false, null, reason);
    }
}
