using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IMigrationRunStore
{
    Task<MigrationRunRecord?> GetAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        MigrationRunRecord run,
        CancellationToken cancellationToken = default);

    Task MarkStartedAsync(
        Guid runId,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(
        Guid runId,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid runId,
        string failureReason,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default);
}
