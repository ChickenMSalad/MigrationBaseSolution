namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalLeaseExpirationService
{
    Task<OperationalExpiredLeaseListResponse> ListExpiredAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalReclaimExpiredLeasesResponse> ReclaimExpiredAsync(
        OperationalReclaimExpiredLeasesRequest request,
        CancellationToken cancellationToken = default);
}
