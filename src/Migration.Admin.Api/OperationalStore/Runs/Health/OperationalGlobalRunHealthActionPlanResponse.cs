namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthActionPlanResponse
{
    public DateTimeOffset GeneratedAt { get; init; }

    public OperationalGlobalRunHealthRecommendationsResponse Recommendations { get; init; } = default!;

    public int ActionCount { get; init; }

    public string OverallPriority { get; init; } = string.Empty;

    public IReadOnlyCollection<OperationalGlobalRunHealthActionPlanItem> Actions { get; init; } =
        Array.Empty<OperationalGlobalRunHealthActionPlanItem>();
}
