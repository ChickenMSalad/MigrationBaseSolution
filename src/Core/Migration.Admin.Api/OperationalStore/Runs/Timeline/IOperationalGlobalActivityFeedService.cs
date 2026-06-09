namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalActivityFeedService
{
    Task<OperationalGlobalActivityFeedResponse> GetRecentActivityAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);
}


