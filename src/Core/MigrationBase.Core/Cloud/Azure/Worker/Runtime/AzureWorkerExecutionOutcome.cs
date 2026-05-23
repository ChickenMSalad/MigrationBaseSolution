namespace MigrationBase.Core.Cloud.Azure.Worker.Runtime;

public sealed record AzureWorkerExecutionOutcome
{
    public required string WorkItemId { get; init; }

    public AzureWorkerExecutionOutcomeKind Kind { get; init; } = AzureWorkerExecutionOutcomeKind.Unknown;

    public AzureWorkerRetryDisposition RetryDisposition { get; init; } = AzureWorkerRetryDisposition.None;

    public int AttemptNumber { get; init; }

    public int MaxAttempts { get; init; }

    public TimeSpan? RetryAfter { get; init; }

    public string? ReasonCode { get; init; }

    public string? Message { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool IsTerminal => Kind is AzureWorkerExecutionOutcomeKind.Completed
        or AzureWorkerExecutionOutcomeKind.NonRetryableFailure
        or AzureWorkerExecutionOutcomeKind.Cancelled
        or AzureWorkerExecutionOutcomeKind.Poisoned;

    public bool ShouldRetry => RetryDisposition == AzureWorkerRetryDisposition.Retry;

    public static AzureWorkerExecutionOutcome Completed(string workItemId, string? message = null)
    {
        return new AzureWorkerExecutionOutcome
        {
            WorkItemId = workItemId,
            Kind = AzureWorkerExecutionOutcomeKind.Completed,
            RetryDisposition = AzureWorkerRetryDisposition.None,
            Message = message
        };
    }

    public static AzureWorkerExecutionOutcome RetryableFailure(
        string workItemId,
        int attemptNumber,
        int maxAttempts,
        TimeSpan? retryAfter,
        string? reasonCode = null,
        string? message = null)
    {
        var disposition = attemptNumber >= maxAttempts
            ? AzureWorkerRetryDisposition.MoveToPoison
            : AzureWorkerRetryDisposition.Retry;

        return new AzureWorkerExecutionOutcome
        {
            WorkItemId = workItemId,
            Kind = AzureWorkerExecutionOutcomeKind.RetryableFailure,
            RetryDisposition = disposition,
            AttemptNumber = attemptNumber,
            MaxAttempts = maxAttempts,
            RetryAfter = disposition == AzureWorkerRetryDisposition.Retry ? retryAfter : null,
            ReasonCode = reasonCode,
            Message = message
        };
    }
}
