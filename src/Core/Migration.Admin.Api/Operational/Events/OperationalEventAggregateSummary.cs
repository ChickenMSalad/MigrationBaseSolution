namespace Migration.Admin.Api.Operational.Events;

public sealed record OperationalEventAggregateSummary(
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int TotalEvents,
    IReadOnlyList<OperationalEventAggregateBucket> BySeverity,
    IReadOnlyList<OperationalEventAggregateBucket> ByCategory,
    IReadOnlyList<OperationalEventAggregateBucket> ByEventType);

public sealed record OperationalEventAggregateBucket(
    string Name,
    int Count);
