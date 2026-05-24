namespace Migration.Workers.QueueExecutor.Services;

public sealed record SqlOperationalWorkItemExecutionResult(
    bool Succeeded,
    string? ResultJson,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsRetryable,
    DateTimeOffset? NextAttemptUtc)
{
    public static SqlOperationalWorkItemExecutionResult Success(string? resultJson = null) =>
        new(true, resultJson, null, null, false, null);

    public static SqlOperationalWorkItemExecutionResult RetryableFailure(
        string errorCode,
        string errorMessage,
        DateTimeOffset? nextAttemptUtc) =>
        new(false, null, errorCode, errorMessage, true, nextAttemptUtc);

    public static SqlOperationalWorkItemExecutionResult TerminalFailure(
        string errorCode,
        string errorMessage) =>
        new(false, null, errorCode, errorMessage, false, null);
}
