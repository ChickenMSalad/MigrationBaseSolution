namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalWorkItemLeaseService
{
    Task<OperationalWorkItemLeaseResponse> LeaseAsync(
        OperationalWorkItemLeaseRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalWorkItemStateTransitionResponse> HeartbeatAsync(
        long workItemId,
        OperationalWorkItemHeartbeatRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalWorkItemStateTransitionResponse> CompleteAsync(
        long workItemId,
        OperationalWorkItemCompleteRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalWorkItemStateTransitionResponse> FailAsync(
        long workItemId,
        OperationalWorkItemFailRequest request,
        CancellationToken cancellationToken = default);
}
