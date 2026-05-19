namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunTimelineMetricsService
{
    Task<OperationalRunTimelineMetricsResponse?> GetMetricsAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}
