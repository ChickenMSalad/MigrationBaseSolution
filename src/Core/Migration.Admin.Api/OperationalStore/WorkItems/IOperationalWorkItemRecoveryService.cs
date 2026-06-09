namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalWorkItemRecoveryService
{
    Task<OperationalWorkItemStateTransitionResponse> ReleaseAsync(
        long workItemId,
        OperationalWorkItemReleaseRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalWorkItemStateTransitionResponse> ResetAsync(
        long workItemId,
        OperationalWorkItemResetRequest request,
        CancellationToken cancellationToken = default);
}


