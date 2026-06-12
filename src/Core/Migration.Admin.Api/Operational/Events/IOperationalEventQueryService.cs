namespace Migration.Admin.Api.Operational.Events;

public interface IOperationalEventQueryService
{
    Task<IReadOnlyList<OperationalEventRecord>> QueryAsync(
        OperationalEventQueryRequest request,
        CancellationToken cancellationToken);

    Task<OperationalEventAggregateSummary> ReadAggregateSummaryAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken);
}


