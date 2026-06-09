namespace Migration.Admin.Api.OperationalStore;

public interface IDispatcherExecutionHistoryQueryService
{
    Task<IReadOnlyCollection<DispatcherExecutionRecord>> QueryAsync(
        DispatcherExecutionHistoryQuery query,
        CancellationToken cancellationToken = default);
}


