using System;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureRetryDecisionEngine : IAzureRetryDecisionEngine
{
    public AzureRetryDecision Decide(AzureRetryDecisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Signal);
        ArgumentNullException.ThrowIfNull(request.Classification);
        ArgumentNullException.ThrowIfNull(request.Policy);

        var currentAttempt = Math.Max(1, request.CurrentAttemptNumber);
        var nextAttempt = currentAttempt + 1;

        if (currentAttempt >= request.Policy.MaxAttempts)
        {
            return AzureRetryDecision.DoNotRetry(
                currentAttempt,
                "Maximum retry attempts have been reached.");
        }

        if (request.Classification.Classification == AzureFailureClassification.Poison)
        {
            return AzureRetryDecision.DoNotRetry(
                currentAttempt,
                "Poison failures are not retryable.");
        }

        if (request.Classification.Classification == AzureFailureClassification.Permanent)
        {
            return AzureRetryDecision.DoNotRetry(
                currentAttempt,
                "Permanent failures are not retryable without correction.");
        }

        if (request.Classification.Classification == AzureFailureClassification.Transient &&
            request.Policy.RetryTransientFailures)
        {
            return AzureRetryDecision.Retry(
                nextAttempt,
                request.Policy.GetDelayForAttempt(currentAttempt),
                "Transient failure is eligible for retry.");
        }

        if (request.Classification.Classification == AzureFailureClassification.Unknown &&
            request.Policy.RetryUnknownFailures)
        {
            return AzureRetryDecision.Retry(
                nextAttempt,
                request.Policy.GetDelayForAttempt(currentAttempt),
                "Unknown failure is allowed by retry policy.");
        }

        if (request.Classification.RetryRecommended)
        {
            return AzureRetryDecision.Retry(
                nextAttempt,
                request.Policy.GetDelayForAttempt(currentAttempt),
                "Failure classification recommended retry.");
        }

        return AzureRetryDecision.DoNotRetry(
            currentAttempt,
            "Retry policy did not allow another attempt.");
    }
}
