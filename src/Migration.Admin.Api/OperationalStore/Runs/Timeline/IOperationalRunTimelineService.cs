namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunTimelineService
{
    Task<OperationalRunTimelineResponse?> GetTimelineAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}
