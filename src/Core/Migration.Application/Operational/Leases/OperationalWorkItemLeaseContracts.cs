namespace Migration.Application.Operational.Leases;

public interface IOperationalWorkItemLeaseCoordinator
{
    Task<OperationalWorkItemLeaseRenewalResult> RenewLeaseAsync(
        RenewOperationalWorkItemLeaseRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalWorkItemLeaseReleaseResult> ReleaseExpiredLeasesAsync(
        ReleaseExpiredOperationalWorkItemLeasesRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalWorkItemLeaseSnapshot> GetLeaseSnapshotAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}

public sealed record RenewOperationalWorkItemLeaseRequest(
    Guid WorkItemId,
    string WorkerId,
    int LeaseSeconds);

public sealed record ReleaseExpiredOperationalWorkItemLeasesRequest(
    Guid? RunId,
    string? WorkerId,
    int MaxItemsToRelease);

public sealed record OperationalWorkItemLeaseRenewalResult(
    Guid WorkItemId,
    string WorkerId,
    bool Renewed,
    DateTimeOffset? LeaseExpiresUtc,
    string? Status,
    string? Message);

public sealed record OperationalWorkItemLeaseReleaseResult(
    int ReleasedCount,
    DateTimeOffset ReleasedUtc);

public sealed record OperationalWorkItemLeaseSnapshot(
    Guid RunId,
    int ActiveLeaseCount,
    int ExpiredLeaseCount,
    int PendingCount,
    int FailedRetryableCount,
    DateTimeOffset? OldestActiveLeaseUtc,
    DateTimeOffset? OldestExpiredLeaseUtc,
    DateTimeOffset SnapshotUtc);
