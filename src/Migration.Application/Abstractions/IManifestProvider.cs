using Migration.Domain.Models;

namespace Migration.Application.Abstractions;

public interface IManifestProvider
{
    string Type { get; }
    Task<IReadOnlyList<ManifestRow>> ReadAsync(MigrationJobDefinition job, CancellationToken cancellationToken = default);
}
