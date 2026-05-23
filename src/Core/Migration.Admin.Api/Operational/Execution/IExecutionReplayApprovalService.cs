namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayApprovalService
{
    Task<ExecutionReplayApprovalResult> ApproveAsync(
        ApproveExecutionReplayRequest request,
        CancellationToken cancellationToken);

    Task<ExecutionReplayApprovalRecord?> FindActiveApprovalAsync(
        Guid sourceExecutionSessionId,
        string scope,
        CancellationToken cancellationToken);

    Task ConsumeAsync(
        Guid replayApprovalId,
        Guid replayExecutionSessionId,
        CancellationToken cancellationToken);
}
