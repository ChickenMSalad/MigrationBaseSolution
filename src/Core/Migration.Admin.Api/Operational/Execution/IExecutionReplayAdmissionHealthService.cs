namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayAdmissionHealthService
{
    Task<ExecutionReplayAdmissionHealthResult> EvaluateAsync(
        EvaluateExecutionReplayAdmissionHealthRequest request,
        CancellationToken cancellationToken);
}


