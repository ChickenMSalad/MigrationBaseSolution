namespace MigrationBase.Core.Cloud.Azure.Workers.Retry;

/// <summary>
/// Describes retry behavior for Azure-hosted migration worker operations.
/// This is a policy contract only; it does not execute retries.
/// </summary>
public sealed record AzureWorkerRetryPolicyDescriptor
{
    public string Name { get; init; } = string.Empty;

    public string WorkloadRole { get; init; } = string.Empty;

    public int MaxAttempts { get; init; }

    public TimeSpan InitialDelay { get; init; }

    public TimeSpan MaxDelay { get; init; }

    public double BackoffMultiplier { get; init; } = 2.0d;

    public bool UseJitter { get; init; } = true;

    public string FailureDisposition { get; init; } = AzureWorkerRetryFailureDispositions.Abandon;

    public IReadOnlyList<string> RetryableFailureClassifications { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> NonRetryableFailureClassifications { get; init; } = Array.Empty<string>();
}
