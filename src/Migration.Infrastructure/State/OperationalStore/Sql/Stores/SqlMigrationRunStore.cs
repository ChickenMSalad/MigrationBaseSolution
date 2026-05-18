using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Microsoft.Extensions.Logging;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores;

public sealed class SqlMigrationRunStore : IMigrationRunStore
{
    private readonly ILogger<SqlMigrationRunStore> _logger;

    public SqlMigrationRunStore(
        ILogger<SqlMigrationRunStore> logger)
    {
        _logger = logger;
    }

    public Task<MigrationRunRecord?> GetAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CreateAsync(
        MigrationRunRecord run,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task MarkStartedAsync(
        Guid runId,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task MarkCompletedAsync(
        Guid runId,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task MarkFailedAsync(
        Guid runId,
        string failureReason,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
