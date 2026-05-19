namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDiagnosticsSummaryResponse
{
    public OperationalRunMetricsResponse Runs { get; init; } = default!;

    public OperationalWorkItemMetricsResponse WorkItems { get; init; } = default!;

    public OperationalLeaseMetricsResponse Leases { get; init; } = default!;

    public DateTimeOffset GeneratedAt { get; init; }
}
