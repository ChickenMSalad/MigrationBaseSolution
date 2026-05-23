namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureReplayEligibilityDecision
{
    private AzureReplayEligibilityDecision(
        bool eligible,
        string? reason,
        string? replayCategory)
    {
        Eligible = eligible;
        Reason = reason;
        ReplayCategory = replayCategory;
    }

    public bool Eligible { get; }

    public string? Reason { get; }

    public string? ReplayCategory { get; }

    public static AzureReplayEligibilityDecision EligibleDecision(
        string replayCategory,
        string? reason = null)
    {
        return new AzureReplayEligibilityDecision(true, reason, replayCategory);
    }

    public static AzureReplayEligibilityDecision NotEligible(string reason)
    {
        return new AzureReplayEligibilityDecision(false, reason, null);
    }
}
