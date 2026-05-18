using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalRunLifecycleService
{
    Task<MigrationRunRecord> CreateRunAsync(
        string sourceSystem,
        string targetSystem,
        CancellationToken cancellationToken = default);

    Task MarkRunStartedAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task MarkRunCompletedAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task MarkRunFailedAsync(
        Guid runId,
        string failureReason,
        CancellationToken cancellationToken = default);
}
