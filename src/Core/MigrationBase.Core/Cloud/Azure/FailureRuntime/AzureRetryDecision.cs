using System;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureRetryDecision
{
    private AzureRetryDecision(
        bool shouldRetry,
        int nextAttemptNumber,
        TimeSpan? delay,
        string? reason)
    {
        ShouldRetry = shouldRetry;
        NextAttemptNumber = nextAttemptNumber;
        Delay = delay;
        Reason = reason;
    }

    public bool ShouldRetry { get; }

    public int NextAttemptNumber { get; }

    public TimeSpan? Delay { get; }

    public string? Reason { get; }

    public static AzureRetryDecision Retry(
        int nextAttemptNumber,
        TimeSpan delay,
        string? reason = null)
    {
        return new AzureRetryDecision(true, nextAttemptNumber, delay, reason);
    }

    public static AzureRetryDecision DoNotRetry(
        int currentAttemptNumber,
        string? reason = null)
    {
        return new AzureRetryDecision(false, currentAttemptNumber, null, reason);
    }
}
