namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalGlobalFailureFilteredAnalyticsService
{
    Task<OperationalGlobalFailureFilteredAnalyticsResponse> GetAnalyticsAsync(
        OperationalGlobalFailureQuery query,
        CancellationToken cancellationToken = default);
}
