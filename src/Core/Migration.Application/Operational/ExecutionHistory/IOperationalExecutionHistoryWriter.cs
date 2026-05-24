namespace Migration.Application.Operational.ExecutionHistory;

public interface IOperationalExecutionHistoryWriter
{
    Task<long> RecordStartedAsync(
        OperationalExecutionAttemptStarted request,
        CancellationToken cancellationToken = default);

    Task RecordCompletedAsync(
        OperationalExecutionAttemptCompleted request,
        CancellationToken cancellationToken = default);

    Task RecordFailedAsync(
        OperationalExecutionAttemptFailed request,
        CancellationToken cancellationToken = default);
}

public sealed record OperationalExecutionAttemptStarted(
    Guid RunId,
    long WorkItemId,
    long? ManifestRowId,
    string WorkItemType,
    string WorkerId,
    int AttemptNumber,
    string? PartitionKey,
    string? PayloadJson,
    DateTimeOffset StartedAtUtc);

public sealed record OperationalExecutionAttemptCompleted(
    long ExecutionAttemptId,
    Guid RunId,
    long WorkItemId,
    string WorkerId,
    string? ResultJson,
    DateTimeOffset CompletedAtUtc);

public sealed record OperationalExecutionAttemptFailed(
    long ExecutionAttemptId,
    Guid RunId,
    long WorkItemId,
    string WorkerId,
    string ErrorCode,
    string ErrorMessage,
    bool IsRetryable,
    string? FailureJson,
    DateTimeOffset FailedAtUtc,
    DateTimeOffset? NextAttemptUtc);
