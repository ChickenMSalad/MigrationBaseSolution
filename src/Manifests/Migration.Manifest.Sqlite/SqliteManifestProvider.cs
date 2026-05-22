using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Manifest.Sqlite;

public sealed class SqliteManifestProvider : IManifestProvider
{
    public string Type => "Sqlite";

    public Task<IReadOnlyList<ManifestRow>> ReadAsync(MigrationJobDefinition job, CancellationToken cancellationToken = default)
    {
        // TODO: Migrate SQLite checkpoint/rate-limiter and manifest support from Ashley.Core/SQLite into this provider.
        return Task.FromResult<IReadOnlyList<ManifestRow>>(Array.Empty<ManifestRow>());
    }
}
