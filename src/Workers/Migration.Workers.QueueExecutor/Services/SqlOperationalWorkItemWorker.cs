using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Operational.ExecutionHistory;
using Migration.Application.Operational.Runs;
using Migration.Application.Operational.WorkItems;
using Migration.Workers.QueueExecutor.Options;

namespace Migration.Workers.QueueExecutor.Services;

public sealed class SqlOperationalWorkItemWorker : BackgroundService
{
    private readonly IOperationalRunCoordinator _runCoordinator;
    private readonly IOperationalWorkItemQueue _workItemQueue;
    private readonly ISqlOperationalWorkItemExecutor _executor;
    private readonly IOperationalExecutionHistoryWriter _executionHistoryWriter;
    private readonly IOptions<SqlOperationalQueueExecutorOptions> _options;
    private readonly ILogger<SqlOperationalWorkItemWorker> _logger;

    public SqlOperationalWorkItemWorker(
        IOperationalRunCoordinator runCoordinator,
        IOperationalWorkItemQueue workItemQueue,
        ISqlOperationalWorkItemExecutor executor,
        IOperationalExecutionHistoryWriter executionHistoryWriter,
        IOptions<SqlOperationalQueueExecutorOptions> options,
        ILogger<SqlOperationalWorkItemWorker> logger)
    {
        _runCoordinator = runCoordinator ?? throw new ArgumentNullException(nameof(runCoordinator));
        _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _executionHistoryWriter = executionHistoryWriter ?? throw new ArgumentNullException(nameof(executionHistoryWriter));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;

        if (!options.Enabled)
        {
            _logger.LogInformation("SQL operational QueueExecutor worker is disabled.");
            return;
        }

        _logger.LogInformation(
            "SQL operational QueueExecutor worker starting. WorkerId={WorkerId}; BatchSize={BatchSize}; LeaseSeconds={LeaseSeconds}; AutoStartRun={AutoStartRun}; RunIdOverride={RunIdOverride}",
            options.WorkerId,
            options.BatchSize,
            options.LeaseSeconds,
            options.AutoStartRun,
            options.RunId);

        if (options.RunId is { } configuredRunId && configuredRunId != Guid.Empty)
        {
            await ExecuteRunAsync(configuredRunId, options, stoppingToken).ConfigureAwait(false);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var runnableRunIds = await _runCoordinator
                .GetRunnableRunIdsAsync(Math.Max(1, options.BatchSize), stoppingToken)
                .ConfigureAwait(false);

            if (runnableRunIds.Count == 0)
            {
                _logger.LogInformation("SQL operational queue idle. No runnable migration runs found.");

                if (options.RunUntilIdleAndStop)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.PollDelaySeconds)), stoppingToken)
                    .ConfigureAwait(false);

                continue;
            }

            foreach (var runId in runnableRunIds)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await ExecuteRunAsync(runId, options, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteRunAsync(
        Guid runId,
        SqlOperationalQueueExecutorOptions options,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SQL operational QueueExecutor worker processing run. RunId={RunId}; WorkerId={WorkerId}; BatchSize={BatchSize}; LeaseSeconds={LeaseSeconds}; AutoStartRun={AutoStartRun}",
            runId,
            options.WorkerId,
            options.BatchSize,
            options.LeaseSeconds,
            options.AutoStartRun);

        if (options.AutoStartRun)
        {
            var startResult = await _runCoordinator.StartRunAsync(new StartOperationalRunRequest(
                runId,
                options.WorkerId,
                options.StartRunBatchSize,
                options.WorkItemType,
                options.PartitionKey,
                options.Priority,
                options.PayloadTemplateJson), stoppingToken).ConfigureAwait(false);

            _logger.LogInformation(
                "SQL operational run start evaluated. RunId={RunId}; Status={Status}; Enqueued={Enqueued}",
                startResult.RunId,
                startResult.Status,
                startResult.EnqueuedWorkItemCount);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var claimed = await _workItemQueue.ClaimAsync(new ClaimOperationalWorkItemsRequest(
                runId,
                options.WorkerId,
                Math.Clamp(options.BatchSize, 1, 500),
                Math.Clamp(options.LeaseSeconds, 30, 3600),
                options.PartitionKey), stoppingToken).ConfigureAwait(false);

            if (claimed.Count == 0)
            {
                var completion = await _runCoordinator.EvaluateCompletionAsync(runId, stoppingToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "SQL operational queue idle. RunId={RunId}; PreviousStatus={PreviousStatus}; CurrentStatus={CurrentStatus}; IsTerminal={IsTerminal}; Message={Message}",
                    completion.RunId,
                    completion.PreviousStatus,
                    completion.CurrentStatus,
                    completion.IsTerminal,
                    completion.Message);

                if (options.RunUntilIdleAndStop || completion.IsTerminal)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.PollDelaySeconds)), stoppingToken)
                    .ConfigureAwait(false);

                continue;
            }

            _logger.LogInformation("Claimed {Count} SQL operational work items for run {RunId}.", claimed.Count, runId);

            foreach (var item in claimed)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await ExecuteClaimedItemAsync(item, options, stoppingToken).ConfigureAwait(false);
            }

            await _runCoordinator.EvaluateCompletionAsync(runId, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteClaimedItemAsync(
        OperationalWorkItemRecord item,
        SqlOperationalQueueExecutorOptions options,
        CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var executionAttemptId = await TryRecordStartedAsync(item, options, startedAtUtc, cancellationToken).ConfigureAwait(false);

        try
        {
            var result = await _executor.ExecuteAsync(item, cancellationToken).ConfigureAwait(false);

            if (result.Succeeded)
            {
                await _workItemQueue.CompleteAsync(new CompleteOperationalWorkItemRequest(
                    item.WorkItemId,
                    options.WorkerId,
                    result.ResultJson), cancellationToken).ConfigureAwait(false);

                await TryRecordCompletedAsync(
                    executionAttemptId,
                    item,
                    options,
                    result.ResultJson,
                    DateTimeOffset.UtcNow,
                    cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Completed SQL operational work item {WorkItemId}.", item.WorkItemId);
                return;
            }

            var errorCode = string.IsNullOrWhiteSpace(result.ErrorCode)
                ? "SQL_OPERATIONAL_WORK_ITEM_FAILED"
                : result.ErrorCode;
            var errorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "SQL operational work item execution failed."
                : result.ErrorMessage;

            await _workItemQueue.FailAsync(new FailOperationalWorkItemRequest(
                item.WorkItemId,
                options.WorkerId,
                errorCode,
                errorMessage,
                result.IsRetryable,
                result.NextAttemptUtc), cancellationToken).ConfigureAwait(false);

            await TryRecordFailedAsync(
                executionAttemptId,
                item,
                options,
                errorCode,
                errorMessage,
                result.IsRetryable,
                result.ResultJson,
                DateTimeOffset.UtcNow,
                result.NextAttemptUtc,
                cancellationToken).ConfigureAwait(false);

            _logger.LogWarning(
                "Failed SQL operational work item {WorkItemId}. Retryable={Retryable}; ErrorCode={ErrorCode}",
                item.WorkItemId,
                result.IsRetryable,
                result.ErrorCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled SQL operational work item execution failure. WorkItemId={WorkItemId}", item.WorkItemId);

            var failedAtUtc = DateTimeOffset.UtcNow;
            var nextAttemptUtc = failedAtUtc.AddSeconds(Math.Max(30, options.RetryDelaySeconds));

            await _workItemQueue.FailAsync(new FailOperationalWorkItemRequest(
                item.WorkItemId,
                options.WorkerId,
                ex.GetType().Name,
                ex.Message,
                true,
                nextAttemptUtc), CancellationToken.None).ConfigureAwait(false);

            await TryRecordFailedAsync(
                executionAttemptId,
                item,
                options,
                ex.GetType().Name,
                ex.Message,
                true,
                BuildFailureJson(ex),
                failedAtUtc,
                nextAttemptUtc,
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task<long?> TryRecordStartedAsync(
        OperationalWorkItemRecord item,
        SqlOperationalQueueExecutorOptions options,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _executionHistoryWriter.RecordStartedAsync(new OperationalExecutionAttemptStarted(
                item.RunId,
                item.WorkItemId,
                item.ManifestRowId,
                item.WorkItemType,
                options.WorkerId,
                item.AttemptCount,
                item.PartitionKey,
                item.PayloadJson,
                startedAtUtc), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unable to record SQL operational execution-history start. WorkItemId={WorkItemId}; RunId={RunId}", item.WorkItemId, item.RunId);
            return null;
        }
    }

    private async Task TryRecordCompletedAsync(
        long? executionAttemptId,
        OperationalWorkItemRecord item,
        SqlOperationalQueueExecutorOptions options,
        string? resultJson,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken)
    {
        if (executionAttemptId is null)
        {
            return;
        }

        try
        {
            await _executionHistoryWriter.RecordCompletedAsync(new OperationalExecutionAttemptCompleted(
                executionAttemptId.Value,
                item.RunId,
                item.WorkItemId,
                options.WorkerId,
                resultJson,
                completedAtUtc), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unable to record SQL operational execution-history completion. WorkItemId={WorkItemId}; RunId={RunId}", item.WorkItemId, item.RunId);
        }
    }

    private async Task TryRecordFailedAsync(
        long? executionAttemptId,
        OperationalWorkItemRecord item,
        SqlOperationalQueueExecutorOptions options,
        string errorCode,
        string errorMessage,
        bool isRetryable,
        string? failureJson,
        DateTimeOffset failedAtUtc,
        DateTimeOffset? nextAttemptUtc,
        CancellationToken cancellationToken)
    {
        if (executionAttemptId is null)
        {
            return;
        }

        try
        {
            await _executionHistoryWriter.RecordFailedAsync(new OperationalExecutionAttemptFailed(
                executionAttemptId.Value,
                item.RunId,
                item.WorkItemId,
                options.WorkerId,
                errorCode,
                errorMessage,
                isRetryable,
                failureJson,
                failedAtUtc,
                nextAttemptUtc), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unable to record SQL operational execution-history failure. WorkItemId={WorkItemId}; RunId={RunId}", item.WorkItemId, item.RunId);
        }
    }

    private static string BuildFailureJson(Exception exception)
    {
        return JsonSerializer.Serialize(new
        {
            exceptionType = exception.GetType().FullName,
            exception.Message,
            exception.StackTrace
        });
    }
}
