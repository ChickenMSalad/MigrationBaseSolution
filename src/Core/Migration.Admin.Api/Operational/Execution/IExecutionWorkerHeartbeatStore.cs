namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionWorkerHeartbeatStore
{
    Task UpsertAsync(
        ExecutionWorkerHeartbeatRequest request,
        CancellationToken cancellationToken);

    Task<ExecutionWorkerTelemetrySummary> ReadSummaryAsync(
        int staleAfterSeconds,
        CancellationToken cancellationToken);
}
