using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Workers.Leasing;

public interface IAzureWorkerLeaseCoordinator
{
    Task<AzureWorkerLeaseAcquisitionResult> TryAcquireAsync(
        AzureWorkerLeaseAcquisitionRequest request,
        CancellationToken cancellationToken = default);

    Task<AzureWorkerLeaseRenewalResult> TryRenewAsync(
        AzureWorkerLeaseRenewalRequest request,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        string leaseId,
        string workerId,
        CancellationToken cancellationToken = default);
}
