namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunTimelineSearchService
{
    Task<OperationalRunTimelineResponse?> SearchAsync(
        Guid runId,
        OperationalRunTimelineSearchQuery query,
        CancellationToken cancellationToken = default);
}
