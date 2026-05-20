namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthSnapshotResponse
{
    public DateTimeOffset SnapshotAt { get; init; }

    public OperationalGlobalRunHealthSummaryResponse Summary { get; init; } = default!;

    public int RecentActivityEventCount { get; init; }

    public int RecentFailureCount { get; init; }

    public int FailureTypeCount { get; init; }

    public int ActiveRiskScore { get; init; }

    public string RiskLevel { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Signals { get; init; } =
        Array.Empty<string>();
}
