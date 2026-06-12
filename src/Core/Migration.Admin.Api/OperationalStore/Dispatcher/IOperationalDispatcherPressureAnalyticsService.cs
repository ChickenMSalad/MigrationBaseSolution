namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalDispatcherPressureAnalyticsService
{
    Task<OperationalDispatcherPressureAnalyticsResponse> GetAnalyticsAsync(
        int metricsSampleLimit = 100,
        CancellationToken cancellationToken = default);
}


