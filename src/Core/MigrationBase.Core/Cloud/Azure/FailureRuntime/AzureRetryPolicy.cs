using System;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureRetryPolicy
{
    public int MaxAttempts { get; init; } = 3;

    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(5);

    public double BackoffMultiplier { get; init; } = 2.0;

    public bool RetryTransientFailures { get; init; } = true;

    public bool RetryUnknownFailures { get; init; }

    public TimeSpan GetDelayForAttempt(int attemptNumber)
    {
        var normalizedAttempt = Math.Max(1, attemptNumber);
        var multiplier = Math.Pow(BackoffMultiplier, normalizedAttempt - 1);
        var calculated = TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * multiplier);

        return calculated <= MaxDelay
            ? calculated
            : MaxDelay;
    }
}
