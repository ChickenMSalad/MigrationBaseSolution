namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRunTimelineQueryService
{
    Task<OperationalRunTimelineResponse?> QueryTimelineAsync(
        Guid runId,
        OperationalRunTimelineQuery query,
        CancellationToken cancellationToken = default);
}


