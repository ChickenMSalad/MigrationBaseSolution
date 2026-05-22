namespace Migration.Admin.Api.Operational.Events;

public interface IOperationalEventStore
{
    Task<Guid> WriteAsync(
        string eventType,
        string severity,
        string category,
        string source,
        string message,
        string? payloadJson,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationalEventRecord>> ReadRecentAsync(
        int take,
        CancellationToken cancellationToken);
}
