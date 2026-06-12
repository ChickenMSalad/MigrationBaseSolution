namespace Migration.Admin.Api.OperationalStore;

public interface IDispatcherExecutionHistoryRetentionService
{
    Task<DispatcherExecutionHistoryRetentionStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default);

    Task<DispatcherExecutionHistoryRetentionPurgeResponse> PurgeEligibleAsync(
        CancellationToken cancellationToken = default);
}


