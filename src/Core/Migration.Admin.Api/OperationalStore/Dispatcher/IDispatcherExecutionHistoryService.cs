namespace Migration.Admin.Api.OperationalStore;

public interface IDispatcherExecutionHistoryService
{
    Task RecordAsync(
        DispatcherExecutionRecord record,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DispatcherExecutionRecord>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task<DispatcherExecutionRecord?> GetAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);
}


