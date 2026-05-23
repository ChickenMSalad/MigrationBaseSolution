namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayPolicyService
{
    Task<ExecutionReplayPolicyEvaluationResult> EvaluateAsync(
        Guid sourceExecutionSessionId,
        string scope,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionReplayPolicyEvaluationRecord>> ReadHistoryAsync(
        Guid sourceExecutionSessionId,
        int take,
        CancellationToken cancellationToken);
}
