using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Queues;

public interface IMigrationRunQueue
{
    Task EnqueueAsync(MigrationRunControlRecord run, CancellationToken cancellationToken = default);
}
