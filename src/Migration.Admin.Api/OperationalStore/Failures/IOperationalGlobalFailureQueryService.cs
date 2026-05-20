namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureQueryService
{
    Task<OperationalGlobalRecentFailuresResponse> QueryRecentFailuresAsync(
        OperationalGlobalFailureQuery query,
        CancellationToken cancellationToken = default);
}
