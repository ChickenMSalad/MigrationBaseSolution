namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalMirrorReadService
{
    Task<IReadOnlyCollection<OperationalMirrorRunSummary>> ListRunsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalMirrorRunDetailResponse?> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}


