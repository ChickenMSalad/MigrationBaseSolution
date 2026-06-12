namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalWorkItemLeaseRequest
{
    public string WorkerId { get; init; } = string.Empty;

    public int Count { get; init; } = 1;
}


