namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchClaimResult
{
    private AzureWorkerDispatchClaimResult(
        bool claimed,
        AzureWorkerDispatchClaim? claim,
        string? reason)
    {
        Claimed = claimed;
        Claim = claim;
        Reason = reason;
    }

    public bool Claimed { get; }

    public AzureWorkerDispatchClaim? Claim { get; }

    public string? Reason { get; }

    public static AzureWorkerDispatchClaimResult Accepted(AzureWorkerDispatchClaim claim)
    {
        return new AzureWorkerDispatchClaimResult(true, claim, null);
    }

    public static AzureWorkerDispatchClaimResult Rejected(string reason)
    {
        return new AzureWorkerDispatchClaimResult(false, null, reason);
    }
}
