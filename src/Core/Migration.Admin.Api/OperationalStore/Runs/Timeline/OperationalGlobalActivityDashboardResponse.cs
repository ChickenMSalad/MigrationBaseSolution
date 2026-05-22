namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalActivityDashboardResponse
{
    public OperationalGlobalActivityFeedResponse RecentActivity { get; init; } = default!;

    public OperationalGlobalActivityMetricsResponse Metrics { get; init; } = default!;

    public OperationalRunTimelineGlobalCatalogResponse Catalog { get; init; } = default!;

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}
