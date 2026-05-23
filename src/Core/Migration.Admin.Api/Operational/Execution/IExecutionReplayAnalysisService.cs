namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayAnalysisService
{
    Task<ExecutionReplayAnalysisResult> AnalyzeAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken);
}
