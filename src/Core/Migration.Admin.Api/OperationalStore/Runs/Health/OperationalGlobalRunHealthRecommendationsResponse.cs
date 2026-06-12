namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthRecommendationsResponse
{
    public DateTimeOffset GeneratedAt { get; init; }

    public OperationalGlobalRunHealthDetailedRiskResponse DetailedRisk { get; init; } = default!;

    public int RecommendationCount { get; init; }

    public IReadOnlyCollection<OperationalGlobalRunHealthRecommendation> Recommendations { get; init; } =
        Array.Empty<OperationalGlobalRunHealthRecommendation>();
}


