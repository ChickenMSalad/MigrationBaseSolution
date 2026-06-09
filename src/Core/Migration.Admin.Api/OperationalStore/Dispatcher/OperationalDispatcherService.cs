using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherService : IOperationalDispatcherService
{
    private readonly IOperationalWorkItemLeaseService _leaseService;
    private readonly IOperationalRunAutoFinalizationService _autoFinalizationService;
    private readonly IOptions<OperationalDispatcherOptions> _options;
    private readonly ILogger<OperationalDispatcherService> _logger;
    private readonly IDispatcherExecutionHistoryService _historyService;

    public OperationalDispatcherService(
        IOperationalWorkItemLeaseService leaseService,
        IOperationalRunAutoFinalizationService autoFinalizationService,
        IOptions<OperationalDispatcherOptions> options,
        ILogger<OperationalDispatcherService> logger,
        IDispatcherExecutionHistoryService historyService)
    {
        _leaseService = leaseService;
        _autoFinalizationService = autoFinalizationService;
        _options = options;
        _logger = logger;
        _historyService = historyService;
    }

    public async Task<OperationalDispatcherRunOnceResponse> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        var executionStartedAt = DateTimeOffset.UtcNow;
        var executionId = Guid.NewGuid();

        var options = NormalizeOptions();

        var lease = await _leaseService.LeaseAsync(
            new OperationalWorkItemLeaseRequest
            {
                WorkerId = options.WorkerId,
                Count = options.LeaseCount
            },
            cancellationToken);

        var completed = 0;
        var failed = 0;
        var workItemIds = new List<long>();

        foreach (var item in lease.WorkItems)
        {
            workItemIds.Add(item.WorkItemId);

            try
            {
                // P3 dispatcher scaffold: execution is intentionally simulated.
                // Real migration execution binding comes in a later set.
                var complete = await _leaseService.CompleteAsync(
                    item.WorkItemId,
                    new OperationalWorkItemCompleteRequest
                    {
                        WorkerId = options.WorkerId
                    },
                    cancellationToken);

                if (complete.Success)
                {
                    completed++;
                }
                else
                {
                    failed++;
                    _logger.LogWarning(
                        "Operational dispatcher could not complete work item {WorkItemId}: {Message}",
                        item.WorkItemId,
                        complete.Message);
                }
            }
            catch (Exception ex)
            {
                failed++;

                _logger.LogError(
                    ex,
                    "Operational dispatcher failed while processing work item {WorkItemId}.",
                    item.WorkItemId);

                try
                {
                    await _leaseService.FailAsync(
                        item.WorkItemId,
                        new OperationalWorkItemFailRequest
                        {
                            WorkerId = options.WorkerId,
                            FailureReason = ex.Message
                        },
                        cancellationToken);
                }
                catch (Exception failEx)
                {
                    _logger.LogError(
                        failEx,
                        "Operational dispatcher failed to mark work item {WorkItemId} failed.",
                        item.WorkItemId);
                }
            }
        }

        if (completed > 0 || failed > 0)
        {
            await _autoFinalizationService.FinalizeEligibleRunsAsync(cancellationToken);
        }

        var executionCompletedAt = DateTimeOffset.UtcNow;

        await _historyService.RecordAsync(
            new DispatcherExecutionRecord
            {
                ExecutionId = executionId,
                WorkerId = options.WorkerId,
                StartedAt = executionStartedAt,
                CompletedAt = executionCompletedAt,
                DurationMilliseconds =
                    (long)(executionCompletedAt - executionStartedAt).TotalMilliseconds,
                RequestedLeaseCount = options.LeaseCount,
                LeasedCount = lease.LeasedCount,
                CompletedCount = completed,
                FailedCount = failed,
                Outcome = failed > 0
                    ? "CompletedWithFailures"
                    : "Completed",
                Message = lease.LeasedCount == 0
                    ? "No eligible work items were leased."
                    : $"Dispatcher processed {completed} completed and {failed} failed work item(s)."
            },
            cancellationToken);

        return new OperationalDispatcherRunOnceResponse
        {
            WorkerId = options.WorkerId,
            RequestedLeaseCount = options.LeaseCount,
            LeasedCount = lease.LeasedCount,
            CompletedCount = completed,
            FailedCount = failed,
            WorkItemIds = workItemIds,
            Message = lease.LeasedCount == 0
                ? "No eligible work items were leased."
                : $"Dispatcher processed {completed} completed and {failed} failed work item(s)."
        };
    }

    private OperationalDispatcherOptions NormalizeOptions()
    {
        var value = _options.Value;

        return new OperationalDispatcherOptions
        {
            Enabled = value.Enabled,
            WorkerId = string.IsNullOrWhiteSpace(value.WorkerId)
                ? "local-dispatcher"
                : value.WorkerId.Trim(),
            PollingIntervalSeconds = Math.Clamp(value.PollingIntervalSeconds, 5, 3600),
            LeaseCount = Math.Clamp(value.LeaseCount, 1, 100),
            SimulateExecution = value.SimulateExecution
        };
    }
}


