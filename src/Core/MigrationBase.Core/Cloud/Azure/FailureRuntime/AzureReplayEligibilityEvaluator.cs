using System;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureReplayEligibilityEvaluator : IAzureReplayEligibilityEvaluator
{
    public AzureReplayEligibilityDecision Evaluate(AzureReplayEligibilityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Signal);
        ArgumentNullException.ThrowIfNull(request.Classification);

        if (request.ReplayGovernancePaused && !request.OperatorOverrideRequested)
        {
            return AzureReplayEligibilityDecision.NotEligible(
                "Replay governance is paused and no operator override was requested.");
        }

        if (request.Classification.Classification == AzureFailureClassification.Poison)
        {
            return AzureReplayEligibilityDecision.NotEligible(
                "Poison failures are not replay eligible.");
        }

        if (request.RetryDecision is not null && request.RetryDecision.ShouldRetry)
        {
            return AzureReplayEligibilityDecision.NotEligible(
                "Failure is still retry eligible and should not be replayed yet.");
        }

        if (request.OperatorOverrideRequested)
        {
            return AzureReplayEligibilityDecision.EligibleDecision(
                "operator-override",
                "Operator override requested replay eligibility.");
        }

        if (request.Classification.ReplayRecommended ||
            request.Classification.Classification == AzureFailureClassification.ReplayEligible)
        {
            return AzureReplayEligibilityDecision.EligibleDecision(
                "classification-recommended",
                "Failure classification recommended replay evaluation.");
        }

        if (request.Classification.Classification == AzureFailureClassification.Transient &&
            request.Signal.AttemptNumber > 1)
        {
            return AzureReplayEligibilityDecision.EligibleDecision(
                "transient-after-retry",
                "Transient failure remained after retry attempts.");
        }

        return AzureReplayEligibilityDecision.NotEligible(
            "No replay eligibility rule matched.");
    }
}
