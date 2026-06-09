namespace Migration.Admin.Api.OperationalStore;

public interface IDispatcherExecutionHistoryMetricsService
{
    Task<DispatcherExecutionHistoryMetricsResponse> GetMetricsAsync(
        CancellationToken cancellationToken = default);
}


