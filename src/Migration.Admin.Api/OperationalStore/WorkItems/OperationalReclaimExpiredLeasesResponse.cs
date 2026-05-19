namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalReclaimExpiredLeasesResponse
{
    public int LeaseTimeoutMinutes { get; init; }

    public DateTimeOffset ExpiresBefore { get; init; }

    public int RequestedMaxCount { get; init; }

    public int ReclaimedCount { get; init; }

    public IReadOnlyCollection<Guid> WorkItemIds { get; init; } =
        Array.Empty<Guid>();

    public string Reason { get; init; } = string.Empty;
}
