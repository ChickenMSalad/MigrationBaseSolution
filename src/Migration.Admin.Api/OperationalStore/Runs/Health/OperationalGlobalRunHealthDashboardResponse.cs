namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthDashboardResponse
{
    public OperationalGlobalRunHealthSummaryResponse HealthSummary { get; init; } = default!;
    public OperationalGlobalActivityDashboardResponse ActivityDashboard { get; init; } = default!;
    public OperationalGlobalFailureAnalyticsDashboardResponse FailureAnalytics { get; init; } = default!;
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyCollection<string> Messages { get; init; } = Array.Empty<string>();
}
