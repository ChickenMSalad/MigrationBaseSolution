using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Infrastructure.State;

public sealed class InMemoryJobStateStore : IJobStateStore
{
    private readonly List<CheckpointRecord> _records = new();

    public Task SaveCheckpointAsync(CheckpointRecord checkpoint, CancellationToken cancellationToken = default)
    {
        _records.Add(checkpoint);
        return Task.CompletedTask;
    }
}
