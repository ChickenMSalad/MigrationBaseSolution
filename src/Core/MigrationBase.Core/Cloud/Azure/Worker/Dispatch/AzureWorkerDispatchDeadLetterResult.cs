namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchDeadLetterResult
{
    private AzureWorkerDispatchDeadLetterResult(
        bool accepted,
        bool claimReleased,
        string? reason)
    {
        Accepted = accepted;
        ClaimReleased = claimReleased;
        Reason = reason;
    }

    public bool Accepted { get; }

    public bool ClaimReleased { get; }

    public string? Reason { get; }

    public static AzureWorkerDispatchDeadLetterResult AcceptedResult(bool claimReleased)
    {
        return new AzureWorkerDispatchDeadLetterResult(true, claimReleased, null);
    }

    public static AzureWorkerDispatchDeadLetterResult Rejected(string reason)
    {
        return new AzureWorkerDispatchDeadLetterResult(false, false, reason);
    }
}
