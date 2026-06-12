namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionLifecycleService
{
    Task<ExecutionPhaseHistoryRecord> TransitionAsync(
        TransitionExecutionPhaseRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionPhaseHistoryRecord>> ReadRecentHistoryAsync(
        Guid executionSessionId,
        int take,
        CancellationToken cancellationToken);
}


