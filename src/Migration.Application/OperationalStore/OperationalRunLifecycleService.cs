using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Application.Models.OperationalStore.Statuses;

namespace Migration.Application.OperationalStore;

public sealed class OperationalRunLifecycleService : IOperationalRunLifecycleService
{
    private readonly IOperationalStore _operationalStore;

    public OperationalRunLifecycleService(
        IOperationalStore operationalStore)
    {
        _operationalStore = operationalStore;
    }

    public async Task<MigrationRunRecord> CreateRunAsync(
        string sourceSystem,
        string targetSystem,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var run = new MigrationRunRecord
        {
            RunId = Guid.NewGuid(),
            SourceSystem = sourceSystem,
            TargetSystem = targetSystem,
            Status = MigrationRunStatuses.Created,
            CreatedAt = now
        };

        await _operationalStore.Runs.CreateAsync(
            run,
            cancellationToken);

        return run;
    }

    public Task MarkRunStartedAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        return _operationalStore.Runs.MarkStartedAsync(
            runId,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public Task MarkRunCompletedAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        return _operationalStore.Runs.MarkCompletedAsync(
            runId,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public async Task MarkRunFailedAsync(
        Guid runId,
        string failureReason,
        CancellationToken cancellationToken = default)
    {
        var failedAt = DateTimeOffset.UtcNow;

        await _operationalStore.Runs.MarkFailedAsync(
            runId,
            failureReason,
            failedAt,
            cancellationToken);

        await _operationalStore.Failures.AddAsync(
            new MigrationFailureRecord
            {
                FailureId = Guid.NewGuid(),
                RunId = runId,
                FailureType = MigrationFailureTypes.RunFailure,
                Message = failureReason,
                IsRetriable = false,
                CreatedAt = failedAt
            },
            cancellationToken);
    }
}
