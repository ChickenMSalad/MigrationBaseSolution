namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayAdmissionService
{
    Task<ExecutionReplayAdmissionEvaluationResult> EvaluateAsync(
        EvaluateExecutionReplayAdmissionRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionReplayAdmissionDecisionRecord>> ReadHistoryAsync(
        Guid executionSessionId,
        int take,
        CancellationToken cancellationToken);
}


