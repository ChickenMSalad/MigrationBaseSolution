namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureReplayEligibilityRequest
{
    public required AzureFailureSignal Signal { get; init; }

    public required AzureFailureClassificationResult Classification { get; init; }

    public AzureRetryDecision? RetryDecision { get; init; }

    public bool OperatorOverrideRequested { get; init; }

    public bool ReplayGovernancePaused { get; init; }
}
