using System;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Infrastructure.Runtime.SqlServer;

public sealed record SqlOperationalQueueRuntimeOptions(
    string WorkerId,
    int BatchSize = 25,
    int LeaseSeconds = 300,
    int RetryDelaySeconds = 300,
    int IdleDelayMilliseconds = 5000,
    int MaxConsecutiveIdlePolls = 0,
    Guid? RunId = null);

public sealed record SqlOperationalWorkItemExecutionResult(
    bool Succeeded,
    bool IsRetryable,
    string? ResultJson = null,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? ExceptionType = null,
    string? FailureJson = null)
{
    public static SqlOperationalWorkItemExecutionResult Complete(string? resultJson = null)
    {
        return new SqlOperationalWorkItemExecutionResult(true, false, resultJson);
    }

    public static SqlOperationalWorkItemExecutionResult RetryableFailure(
        string? errorCode,
        string? errorMessage,
        string? exceptionType = null,
        string? failureJson = null)
    {
        return new SqlOperationalWorkItemExecutionResult(false, true, null, errorCode, errorMessage, exceptionType, failureJson);
    }

    public static SqlOperationalWorkItemExecutionResult TerminalFailure(
        string? errorCode,
        string? errorMessage,
        string? exceptionType = null,
        string? failureJson = null)
    {
        return new SqlOperationalWorkItemExecutionResult(false, false, null, errorCode, errorMessage, exceptionType, failureJson);
    }
}

public sealed record SqlOperationalQueueRuntimeResult(
    int ClaimedCount,
    int CompletedCount,
    int FailedCount,
    int RetryScheduledCount,
    int IdlePollCount);

public delegate Task<SqlOperationalWorkItemExecutionResult> SqlOperationalWorkItemExecutor(
    SqlOperationalWorkItem workItem,
    CancellationToken cancellationToken);
