namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureReplayAdmissionDecision
{
    private AzureReplayAdmissionDecision(
        bool admitted,
        string? reason)
    {
        Admitted = admitted;
        Reason = reason;
    }

    public bool Admitted { get; }

    public string? Reason { get; }

    public static AzureReplayAdmissionDecision Admit(string? reason = null)
    {
        return new AzureReplayAdmissionDecision(true, reason);
    }

    public static AzureReplayAdmissionDecision Reject(string reason)
    {
        return new AzureReplayAdmissionDecision(false, reason);
    }
}
