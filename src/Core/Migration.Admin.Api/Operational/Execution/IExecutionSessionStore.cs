namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionSessionStore
{
    Task<ExecutionSessionRecord> CreateAsync(
        CreateExecutionSessionRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionSessionRecord>> ReadRecentAsync(
        int take,
        CancellationToken cancellationToken);
}


