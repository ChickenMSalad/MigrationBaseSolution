namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayPolicyService
{
    Task<ExecutionReplayPolicyEvaluationResult> EvaluateAsync(
        Guid sourceExecutionSessionId,
        string scope,
        CancellationToken cancellationToken);
}
