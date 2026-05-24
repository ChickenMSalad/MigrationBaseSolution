using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Operational.Runs;
using Migration.Application.Operational.WorkItems;
using Migration.Workers.QueueExecutor.Options;

namespace Migration.Workers.QueueExecutor.Services;

public sealed class SqlOperationalWorkItemWorker : BackgroundService
{
    private readonly IOperationalRunCoordinator _runCoordinator;
    private readonly IOperationalWorkItemQueue _workItemQueue;
    private readonly ISqlOperationalWorkItemExecutor _executor;
    private readonly IOptions<SqlOperationalQueueExecutorOptions> _options;
    private readonly ILogger<SqlOperationalWorkItemWorker> _logger;

    public SqlOperationalWorkItemWorker(
        IOperationalRunCoordinator runCoordinator,
        IOperationalWorkItemQueue workItemQueue,
        ISqlOperationalWorkItemExecutor executor,
        IOptions<SqlOperationalQueueExecutorOptions> options,
        ILogger<SqlOperationalWorkItemWorker> logger)
    {
        _runCoordinator = runCoordinator ?? throw new ArgumentNullException(nameof(runCoordinator));
        _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
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

        if (options.RunId is null || options.RunId == Guid.Empty)
        {
            throw new InvalidOperationException("SqlOperationalQueueExecutor:RunId must be configured when SqlOperationalQueueExecutor:Enabled is true.");
        }

        var runId = options.RunId.Value;
        _logger.LogInformation(
            "SQL operational QueueExecutor worker starting. RunId={RunId}; WorkerId={WorkerId}; BatchSize={BatchSize}; LeaseSeconds={LeaseSeconds}; AutoStartRun={AutoStartRun}",
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
                "SQL operational run start evaluated. RunId={RunId}; Status={Status}; Enqueued={Enqueued}; Selected={Selected}",
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

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.PollDelaySeconds)), stoppingToken).ConfigureAwait(false);
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
        try
        {
            var result = await _executor.ExecuteAsync(item, cancellationToken).ConfigureAwait(false);

            if (result.Succeeded)
            {
                await _workItemQueue.CompleteAsync(new CompleteOperationalWorkItemRequest(
                    item.WorkItemId,
                    options.WorkerId,
                    result.ResultJson), cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Completed SQL operational work item {WorkItemId}.", item.WorkItemId);
                return;
            }

            await _workItemQueue.FailAsync(new FailOperationalWorkItemRequest(
                item.WorkItemId,
                options.WorkerId,
                string.IsNullOrWhiteSpace(result.ErrorCode) ? "SQL_OPERATIONAL_WORK_ITEM_FAILED" : result.ErrorCode,
                string.IsNullOrWhiteSpace(result.ErrorMessage) ? "SQL operational work item execution failed." : result.ErrorMessage,
                result.IsRetryable,
                result.NextAttemptUtc), cancellationToken).ConfigureAwait(false);

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

            await _workItemQueue.FailAsync(new FailOperationalWorkItemRequest(
                item.WorkItemId,
                options.WorkerId,
                ex.GetType().Name,
                ex.Message,
                true,
                DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, options.RetryDelaySeconds))), CancellationToken.None).ConfigureAwait(false);
        }
    }
}
