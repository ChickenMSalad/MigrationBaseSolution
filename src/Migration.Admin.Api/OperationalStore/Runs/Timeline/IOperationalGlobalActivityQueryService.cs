namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalActivityQueryService
{
    Task<OperationalGlobalActivityFeedResponse> QueryRecentActivityAsync(
        OperationalGlobalActivityQuery query,
        CancellationToken cancellationToken = default);
}
