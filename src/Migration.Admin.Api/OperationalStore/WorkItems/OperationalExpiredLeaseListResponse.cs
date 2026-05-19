namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalExpiredLeaseListResponse
{
    public int LeaseTimeoutMinutes { get; init; }

    public DateTimeOffset ExpiresBefore { get; init; }

    public int Count { get; init; }

    public IReadOnlyCollection<OperationalExpiredLeaseItem> WorkItems { get; init; } =
        Array.Empty<OperationalExpiredLeaseItem>();
}
