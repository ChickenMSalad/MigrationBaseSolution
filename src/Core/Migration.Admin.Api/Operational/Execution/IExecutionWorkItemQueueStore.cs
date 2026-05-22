namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionWorkItemQueueStore
{
    Task<IReadOnlyList<ExecutionWorkItemRecord>> ExpandFromPlanAsync(
        ExpandExecutionPlanToWorkItemsRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionWorkItemRecord>> LeaseAsync(
        LeaseExecutionWorkItemsRequest request,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        CompleteExecutionWorkItemRequest request,
        CancellationToken cancellationToken);

    Task FailAsync(
        FailExecutionWorkItemRequest request,
        CancellationToken cancellationToken);

    Task<ExecutionWorkItemQueueSummary> ReadSummaryAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionWorkItemRecord>> ReadRecentAsync(
        Guid executionSessionId,
        int take,
        CancellationToken cancellationToken);
}
