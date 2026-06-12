namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherDashboardSummaryResponse
{
    public OperationalDispatcherStatusResponse Dispatcher { get; init; } = default!;

    public OperationalDispatcherDiagnosticsResponse Diagnostics { get; init; } = default!;

    public DispatcherExecutionHistoryMetricsResponse ExecutionMetrics { get; init; } = default!;

    public DispatcherExecutionHistoryRetentionStatusResponse Retention { get; init; } = default!;

    public DispatcherExecutionHistoryReadinessResponse ExecutionHistoryReadiness { get; init; } = default!;

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}


