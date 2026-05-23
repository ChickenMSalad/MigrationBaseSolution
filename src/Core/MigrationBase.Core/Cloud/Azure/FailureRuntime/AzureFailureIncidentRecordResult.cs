namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureIncidentRecordResult
{
    private AzureFailureIncidentRecordResult(
        bool recorded,
        AzureFailureIncidentRecord? record,
        string? reason)
    {
        Recorded = recorded;
        Record = record;
        Reason = reason;
    }

    public bool Recorded { get; }

    public AzureFailureIncidentRecord? Record { get; }

    public string? Reason { get; }

    public static AzureFailureIncidentRecordResult Success(AzureFailureIncidentRecord record)
    {
        return new AzureFailureIncidentRecordResult(true, record, null);
    }

    public static AzureFailureIncidentRecordResult Rejected(string reason)
    {
        return new AzureFailureIncidentRecordResult(false, null, reason);
    }
}
