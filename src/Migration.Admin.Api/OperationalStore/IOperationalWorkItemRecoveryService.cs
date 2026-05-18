namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalWorkItemRecoveryService
{
    Task<OperationalWorkItemStateTransitionResponse> ReleaseAsync(
        Guid workItemId,
        OperationalWorkItemReleaseRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalWorkItemStateTransitionResponse> ResetAsync(
        Guid workItemId,
        OperationalWorkItemResetRequest request,
        CancellationToken cancellationToken = default);
}
