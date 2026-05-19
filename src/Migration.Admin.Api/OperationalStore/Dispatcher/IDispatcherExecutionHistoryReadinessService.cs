namespace Migration.Admin.Api.OperationalStore;

public interface IDispatcherExecutionHistoryReadinessService
{
    Task<DispatcherExecutionHistoryReadinessResponse> CheckAsync(
        CancellationToken cancellationToken = default);
}
