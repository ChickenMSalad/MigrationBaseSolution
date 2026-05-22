using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Manifest.Sql;

public sealed class SqlManifestProvider : IManifestProvider
{
    public string Type => "Sql";

    public Task<IReadOnlyList<ManifestRow>> ReadAsync(MigrationJobDefinition job, CancellationToken cancellationToken = default)
    {
        // TODO: Migrate the SQL Server manifest/query implementation from Ashley.Core/SQL into this provider.
        return Task.FromResult<IReadOnlyList<ManifestRow>>(Array.Empty<ManifestRow>());
    }
}
