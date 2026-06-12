namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsDashboardResponse
{
    public OperationalGlobalFailureDashboardResponse Dashboard { get; init; } = default!;

    public OperationalGlobalFailureSystemPairMetricsResponse SystemPairMetrics { get; init; } = default!;

    public OperationalGlobalFailureRunStatusMetricsResponse RunStatusMetrics { get; init; } = default!;

    public OperationalGlobalFailureCatalogResponse Catalog { get; init; } = default!;

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}


