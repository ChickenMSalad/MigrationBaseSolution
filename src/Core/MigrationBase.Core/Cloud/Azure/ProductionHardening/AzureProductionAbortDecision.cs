namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionAbortDecision
{
    private AzureProductionAbortDecision(
        bool abort,
        string reason)
    {
        Abort = abort;
        Reason = reason;
    }

    public bool Abort { get; }

    public string Reason { get; }

    public static AzureProductionAbortDecision Approved(string reason)
    {
        return new AzureProductionAbortDecision(true, reason);
    }

    public static AzureProductionAbortDecision Rejected(string reason)
    {
        return new AzureProductionAbortDecision(false, reason);
    }
}
