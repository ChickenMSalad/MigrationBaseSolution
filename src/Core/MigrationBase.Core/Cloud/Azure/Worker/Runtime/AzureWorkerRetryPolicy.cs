namespace MigrationBase.Core.Cloud.Azure.Worker.Runtime;

public sealed record AzureWorkerRetryPolicy
{
    public const int DefaultMaxAttempts = 5;

    public int MaxAttempts { get; init; } = DefaultMaxAttempts;

    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(5);

    public double BackoffMultiplier { get; init; } = 2.0d;

    public bool UseJitter { get; init; } = true;

    public TimeSpan CalculateDelay(int nextAttemptNumber)
    {
        if (nextAttemptNumber <= 1)
        {
            return InitialDelay;
        }

        var multiplier = Math.Pow(BackoffMultiplier, nextAttemptNumber - 1);
        var calculatedTicks = InitialDelay.Ticks * multiplier;
        var cappedTicks = Math.Min((long)calculatedTicks, MaxDelay.Ticks);
        return TimeSpan.FromTicks(Math.Max(InitialDelay.Ticks, cappedTicks));
    }

    public bool AllowsRetry(int attemptNumber)
    {
        return attemptNumber < MaxAttempts;
    }
}
