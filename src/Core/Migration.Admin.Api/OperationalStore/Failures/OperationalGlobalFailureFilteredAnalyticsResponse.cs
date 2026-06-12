namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureFilteredAnalyticsResponse
{
    public OperationalGlobalFailureQuery Query { get; init; } = default!;

    public OperationalGlobalRecentFailuresResponse Results { get; init; } = default!;

    public OperationalGlobalFailureMetricsResponse Metrics { get; init; } = default!;

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}


