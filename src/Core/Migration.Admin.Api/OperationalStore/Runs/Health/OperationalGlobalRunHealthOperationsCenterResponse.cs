namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthOperationsCenterResponse
{
    public DateTimeOffset GeneratedAt { get; init; }

    public OperationalGlobalRunHealthDashboardResponse Dashboard { get; init; } = default!;

    public OperationalGlobalRunHealthDetailedRiskResponse DetailedRisk { get; init; } = default!;

    public OperationalGlobalRunHealthRecommendationsResponse Recommendations { get; init; } = default!;

    public OperationalGlobalRunHealthActionPlanResponse ActionPlan { get; init; } = default!;

    public string OverallPriority { get; init; } = string.Empty;

    public IReadOnlyCollection<string> SummaryMessages { get; init; } =
        Array.Empty<string>();
}


