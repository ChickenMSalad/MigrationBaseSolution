using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Infrastructure.Runtime.SqlServer;

public sealed class SqlOperationalQueueRuntime
{
    private readonly SqlOperationalQueueStore _queueStore;
    private readonly SqlOperationalWorkItemExecutor _executor;

    public SqlOperationalQueueRuntime(
        SqlOperationalQueueStore queueStore,
        SqlOperationalWorkItemExecutor executor)
    {
        _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public async Task<SqlOperationalQueueRuntimeResult> RunUntilIdleAsync(
        SqlOperationalQueueRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        ValidateOptions(options);

        int claimed = 0;
        int completed = 0;
        int failed = 0;
        int retryScheduled = 0;
        int idlePolls = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            IReadOnlyList<SqlOperationalWorkItem> batch = await _queueStore.ClaimWorkItemsAsync(
                new SqlClaimWorkItemsRequest(
                    options.WorkerId,
                    options.BatchSize,
                    options.LeaseSeconds,
                    options.RunId),
                cancellationToken).ConfigureAwait(false);

            if (batch.Count == 0)
            {
                idlePolls++;
                if (options.MaxConsecutiveIdlePolls <= 0 || idlePolls >= options.MaxConsecutiveIdlePolls)
                {
                    break;
                }

                await DelayIfNeededAsync(options.IdleDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                continue;
            }

            idlePolls = 0;
            claimed += batch.Count;

            foreach (SqlOperationalWorkItem workItem in batch)
            {
                SqlOperationalWorkItemExecutionResult result;
                try
                {
                    result = await _executor(workItem, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result = SqlOperationalWorkItemExecutionResult.RetryableFailure(
                        "UnhandledException",
                        ex.Message,
                        ex.GetType().FullName,
                        null);
                }

                if (result.Succeeded)
                {
                    await _queueStore.CompleteWorkItemAsync(
                        workItem.WorkItemId,
                        options.WorkerId,
                        result.ResultJson,
                        cancellationToken).ConfigureAwait(false);
                    completed++;
                    continue;
                }

                await _queueStore.FailWorkItemAsync(
                    new SqlFailWorkItemRequest(
                        workItem.WorkItemId,
                        options.WorkerId,
                        result.ErrorCode,
                        result.ErrorMessage,
                        result.ExceptionType,
                        result.IsRetryable,
                        options.RetryDelaySeconds,
                        result.FailureJson),
                    cancellationToken).ConfigureAwait(false);

                if (result.IsRetryable)
                {
                    retryScheduled++;
                }
                else
                {
                    failed++;
                }
            }
        }

        return new SqlOperationalQueueRuntimeResult(claimed, completed, failed, retryScheduled, idlePolls);
    }

    public async Task<SqlOperationalQueueRuntimeResult> RunContinuousAsync(
        SqlOperationalQueueRuntimeOptions options,
        CancellationToken cancellationToken)
    {
        ValidateOptions(options);

        int claimed = 0;
        int completed = 0;
        int failed = 0;
        int retryScheduled = 0;
        int idlePolls = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            SqlOperationalQueueRuntimeResult cycle = await RunUntilIdleAsync(
                options with { MaxConsecutiveIdlePolls = 1 },
                cancellationToken).ConfigureAwait(false);

            claimed += cycle.ClaimedCount;
            completed += cycle.CompletedCount;
            failed += cycle.FailedCount;
            retryScheduled += cycle.RetryScheduledCount;

            if (cycle.ClaimedCount == 0)
            {
                idlePolls++;
                await DelayIfNeededAsync(options.IdleDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }

        return new SqlOperationalQueueRuntimeResult(claimed, completed, failed, retryScheduled, idlePolls);
    }

    private static void ValidateOptions(SqlOperationalQueueRuntimeOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.WorkerId))
        {
            throw new ArgumentException("WorkerId is required.", nameof(options));
        }

        if (options.BatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "BatchSize must be greater than zero.");
        }
    }

    private static Task DelayIfNeededAsync(int delayMilliseconds, CancellationToken cancellationToken)
    {
        if (delayMilliseconds <= 0)
        {
            return Task.CompletedTask;
        }

        return Task.Delay(delayMilliseconds, cancellationToken);
    }
}
