using Microsoft.Extensions.Logging;
using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Queues;

public sealed class NullMigrationRunQueue : IMigrationRunQueue
{
    private readonly ILogger<NullMigrationRunQueue> _logger;

    public NullMigrationRunQueue(ILogger<NullMigrationRunQueue> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync(MigrationRunControlRecord run, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Migration run {RunId} was saved but not queued because MigrationRunQueue:Provider is None.", run.RunId);
        return Task.CompletedTask;
    }
}
