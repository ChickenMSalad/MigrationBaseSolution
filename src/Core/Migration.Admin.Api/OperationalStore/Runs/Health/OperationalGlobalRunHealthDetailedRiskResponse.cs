namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthDetailedRiskResponse
{
    public DateTimeOffset GeneratedAt { get; init; }

    public OperationalGlobalRunHealthTrendSummaryResponse TrendSummary { get; init; } = default!;

    public int RiskScore { get; init; }

    public string RiskLevel { get; init; } = string.Empty;

    public string RiskPosture { get; init; } = string.Empty;

    public IReadOnlyCollection<OperationalGlobalRunHealthRiskBucket> Buckets { get; init; } =
        Array.Empty<OperationalGlobalRunHealthRiskBucket>();

    public IReadOnlyCollection<string> Recommendations { get; init; } =
        Array.Empty<string>();
}


