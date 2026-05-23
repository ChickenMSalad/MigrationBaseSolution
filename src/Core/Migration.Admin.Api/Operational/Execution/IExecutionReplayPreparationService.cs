namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayPreparationService
{
    Task<ExecutionReplayPreparationResult> PrepareAsync(
        PrepareExecutionReplayRequest request,
        CancellationToken cancellationToken);
}
