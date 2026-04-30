using Migration.Domain.Models;

namespace Migration.Application.Abstractions;

public interface IJobStateStore
{
    Task SaveCheckpointAsync(CheckpointRecord checkpoint, CancellationToken cancellationToken = default);
}
