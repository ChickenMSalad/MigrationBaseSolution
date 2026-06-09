namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayMaterializationService
{
    Task<ExecutionReplayMaterializationResult> MaterializeAsync(
        MaterializeExecutionReplayRequest request,
        CancellationToken cancellationToken);
}


