namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalWorkItemLeaseService
{
    Task<OperationalWorkItemLeaseResponse> LeaseAsync(
        OperationalWorkItemLeaseRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalWorkItemStateTransitionResponse> HeartbeatAsync(
        Guid workItemId,
        OperationalWorkItemHeartbeatRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalWorkItemStateTransitionResponse> CompleteAsync(
        Guid workItemId,
        OperationalWorkItemCompleteRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalWorkItemStateTransitionResponse> FailAsync(
        Guid workItemId,
        OperationalWorkItemFailRequest request,
        CancellationToken cancellationToken = default);
}
