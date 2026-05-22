namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthTrendSummaryResponse
{
    public DateTimeOffset GeneratedAt { get; init; }

    public OperationalGlobalRunHealthSnapshotResponse CurrentSnapshot { get; init; } = default!;

    public string TrendDirection { get; init; } = string.Empty;

    public string TrendMessage { get; init; } = string.Empty;

    public int RiskScore { get; init; }

    public string RiskLevel { get; init; } = string.Empty;

    public int RecentActivityEventCount { get; init; }

    public int RecentFailureCount { get; init; }

    public int ActiveRunCount { get; init; }

    public int OutstandingWorkItemCount { get; init; }

    public int LockedWorkItemCount { get; init; }

    public IReadOnlyCollection<OperationalGlobalRunHealthTrendSignal> Signals { get; init; } =
        Array.Empty<OperationalGlobalRunHealthTrendSignal>();
}
