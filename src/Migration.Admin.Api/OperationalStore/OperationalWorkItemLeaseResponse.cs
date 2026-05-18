namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalWorkItemLeaseResponse
{
    public string WorkerId { get; init; } = string.Empty;

    public int RequestedCount { get; init; }

    public int LeasedCount { get; init; }

    public IReadOnlyCollection<OperationalWorkItemLeaseItem> WorkItems { get; init; } =
        Array.Empty<OperationalWorkItemLeaseItem>();
}
