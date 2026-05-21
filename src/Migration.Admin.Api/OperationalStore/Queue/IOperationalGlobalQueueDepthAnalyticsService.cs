namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalQueueDepthAnalyticsService
{
    Task<OperationalGlobalQueueDepthAnalyticsResponse> GetAnalyticsAsync(
        CancellationToken cancellationToken = default);
}
