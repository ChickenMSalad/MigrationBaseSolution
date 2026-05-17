using Microsoft.Extensions.Configuration;

namespace Migration.ControlPlane.Queues;

public sealed class QueueExecutionReadinessService : IQueueExecutionReadinessService
{
    private readonly IConfiguration _configuration;
    private readonly IQueueDispatchProvider _dispatchProvider;
    private readonly IQueueReceiveProvider _receiveProvider;
    private readonly IQueueExecutionObservabilityService _observabilityService;

    public QueueExecutionReadinessService(
        IConfiguration configuration,
        IQueueDispatchProvider dispatchProvider,
        IQueueReceiveProvider receiveProvider,
        IQueueExecutionObservabilityService observabilityService)
    {
        _configuration = configuration;
        _dispatchProvider = dispatchProvider;
        _receiveProvider = receiveProvider;
        _observabilityService = observabilityService;
    }

    public QueueExecutionReadinessSnapshot GetSnapshot()
    {
        var loopOptions = QueueWorkerLoopPlanner.BuildOptions(_configuration);
        var workerLoop = QueueWorkerLoopPlanner.BuildDescriptor(
            loopOptions,
            _receiveProvider.Descriptor);

        var poisonOptions = QueuePoisonHandlingPlanner.BuildOptions(_configuration);
        var poisonPlan = QueuePoisonHandlingPlanner.BuildPlan(
            poisonOptions,
            _receiveProvider.Descriptor);

        var observability = _observabilityService.GetSnapshot();

        var blocking = new List<string>();
        var warnings = new List<string>();

        if (!_dispatchProvider.Descriptor.IsConfigured)
        {
            warnings.Add("Queue dispatch provider is not configured.");
        }

        if (!_receiveProvider.Descriptor.IsConfigured)
        {
            blocking.Add("Queue receive provider is not configured.");
        }

        if (!loopOptions.Enabled)
        {
            warnings.Add("Queue worker loop is disabled.");
        }

        if (loopOptions.DryRun)
        {
            warnings.Add("Queue worker loop is in dry-run mode.");
        }

        if (observability.CoordinatorDryRun)
        {
            warnings.Add("Queue executor coordinator is in dry-run mode.");
        }

        if (!observability.CompleteMessages)
        {
            warnings.Add("Queue messages will not be completed/deleted.");
        }

        foreach (var warning in workerLoop.Warnings)
        {
            AddUnique(warnings, warning);
        }

        foreach (var warning in poisonPlan.Warnings)
        {
            AddUnique(warnings, warning);
        }

        foreach (var warning in observability.Warnings)
        {
            AddUnique(warnings, warning);
        }

        var readyForDryRun = _receiveProvider.Descriptor.IsConfigured ||
                             _receiveProvider.Descriptor.ProviderKind.Equals("inMemory", StringComparison.OrdinalIgnoreCase);

        var readyForLive = _receiveProvider.Descriptor.IsConfigured &&
                           loopOptions.Enabled &&
                           !loopOptions.DryRun &&
                           !observability.CoordinatorDryRun &&
                           observability.CompleteMessages &&
                           blocking.Count == 0;

        return new QueueExecutionReadinessSnapshot(
            GeneratedUtc: DateTimeOffset.UtcNow,
            IsReadyForDryRun: readyForDryRun,
            IsReadyForLiveExecution: readyForLive,
            DispatchProvider: _dispatchProvider.Descriptor,
            ReceiveProvider: _receiveProvider.Descriptor,
            WorkerLoop: workerLoop,
            PoisonHandling: poisonPlan,
            Observability: observability,
            BlockingIssues: blocking,
            Warnings: warnings);
    }

    private static void AddUnique(List<string> values, string value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            !values.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            values.Add(value);
        }
    }
}
