namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalDispatcherDiagnosticsResponse
{
    public int EligibleWorkItemCount { get; init; }

    public int BlockedByRunStateCount { get; init; }

    public int LockedWorkItemCount { get; init; }

    public int CompletedWorkItemCount { get; init; }

    public int FailedWorkItemCount { get; init; }

    public int ExpiredLeaseCount { get; init; }

    public IReadOnlyCollection<OperationalDispatcherEligibleWorkItemPreview> EligiblePreview { get; init; } =
        Array.Empty<OperationalDispatcherEligibleWorkItemPreview>();

    public string Message { get; init; } = string.Empty;
}


