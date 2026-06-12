namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayLineageService
{
    Task<ExecutionReplayLineageResult> ReadLineageAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken);
}


