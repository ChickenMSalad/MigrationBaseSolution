namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public interface IAzureWorkerDispatchClaimStore
{
    Task<AzureWorkerDispatchClaimResult> TryClaimAsync(
        AzureWorkerDispatchClaimRequest request,
        CancellationToken cancellationToken);

    Task<bool> ReleaseAsync(
        AzureWorkerDispatchClaim claim,
        string reason,
        CancellationToken cancellationToken);
}
