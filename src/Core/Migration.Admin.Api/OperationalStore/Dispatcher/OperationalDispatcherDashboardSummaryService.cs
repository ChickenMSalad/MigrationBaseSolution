using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherDashboardSummaryService
    : IOperationalDispatcherDashboardSummaryService
{
    private readonly IOptions<OperationalDispatcherOptions> _dispatcherOptions;
    private readonly IOperationalDispatcherDiagnosticsService _diagnosticsService;
    private readonly IDispatcherExecutionHistoryMetricsService _metricsService;
    private readonly IDispatcherExecutionHistoryRetentionService _retentionService;
    private readonly IDispatcherExecutionHistoryReadinessService _readinessService;

    public OperationalDispatcherDashboardSummaryService(
        IOptions<OperationalDispatcherOptions> dispatcherOptions,
        IOperationalDispatcherDiagnosticsService diagnosticsService,
        IDispatcherExecutionHistoryMetricsService metricsService,
        IDispatcherExecutionHistoryRetentionService retentionService,
        IDispatcherExecutionHistoryReadinessService readinessService)
    {
        _dispatcherOptions = dispatcherOptions;
        _diagnosticsService = diagnosticsService;
        _metricsService = metricsService;
        _retentionService = retentionService;
        _readinessService = readinessService;
    }

    public async Task<OperationalDispatcherDashboardSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var dispatcherOptions = _dispatcherOptions.Value;

        var dispatcher = new OperationalDispatcherStatusResponse
        {
            Enabled = dispatcherOptions.Enabled,
            WorkerId = string.IsNullOrWhiteSpace(dispatcherOptions.WorkerId)
                ? "local-dispatcher"
                : dispatcherOptions.WorkerId,
            PollingIntervalSeconds = dispatcherOptions.PollingIntervalSeconds,
            LeaseCount = dispatcherOptions.LeaseCount,
            SimulateExecution = dispatcherOptions.SimulateExecution,
            Mode = dispatcherOptions.Enabled ? "Enabled" : "Disabled"
        };

        var diagnostics = await _diagnosticsService.GetDiagnosticsAsync(
            cancellationToken);

        var executionMetrics = await _metricsService.GetMetricsAsync(
            cancellationToken);

        var retention = await _retentionService.GetStatusAsync(
            cancellationToken);

        var readiness = await _readinessService.CheckAsync(
            cancellationToken);

        var messages = BuildMessages(
            dispatcher,
            diagnostics,
            executionMetrics,
            retention,
            readiness);

        return new OperationalDispatcherDashboardSummaryResponse
        {
            Dispatcher = dispatcher,
            Diagnostics = diagnostics,
            ExecutionMetrics = executionMetrics,
            Retention = retention,
            ExecutionHistoryReadiness = readiness,
            GeneratedAt = DateTimeOffset.UtcNow,
            Messages = messages
        };
    }

    private static IReadOnlyCollection<string> BuildMessages(
        OperationalDispatcherStatusResponse dispatcher,
        OperationalDispatcherDiagnosticsResponse diagnostics,
        DispatcherExecutionHistoryMetricsResponse executionMetrics,
        DispatcherExecutionHistoryRetentionStatusResponse retention,
        DispatcherExecutionHistoryReadinessResponse readiness)
    {
        var messages = new List<string>();

        messages.Add(dispatcher.Enabled
            ? "Operational dispatcher is enabled."
            : "Operational dispatcher is disabled.");

        messages.Add(readiness.Ready
            ? "Dispatcher execution history is ready."
            : "Dispatcher execution history is not ready.");

        messages.Add(diagnostics.EligibleWorkItemCount == 0
            ? "No dispatcher work items are currently eligible."
            : $"{diagnostics.EligibleWorkItemCount} dispatcher work item(s) are currently eligible.");

        messages.Add($"{executionMetrics.TotalExecutionCount} dispatcher execution(s) have been recorded.");

        messages.Add(retention.Enabled
            ? "Dispatcher execution history retention is enabled."
            : "Dispatcher execution history retention is disabled.");

        return messages;
    }
}


