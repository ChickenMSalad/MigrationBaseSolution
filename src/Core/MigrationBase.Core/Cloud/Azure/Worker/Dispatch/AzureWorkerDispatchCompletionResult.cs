namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchCompletionResult
{
    private AzureWorkerDispatchCompletionResult(
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

    public static AzureWorkerDispatchCompletionResult Completed(bool claimReleased)
    {
        return new AzureWorkerDispatchCompletionResult(true, claimReleased, null);
    }

    public static AzureWorkerDispatchCompletionResult Rejected(string reason)
    {
        return new AzureWorkerDispatchCompletionResult(false, false, reason);
    }
}
