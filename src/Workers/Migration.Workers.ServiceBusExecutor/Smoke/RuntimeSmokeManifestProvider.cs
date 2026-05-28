using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Workers.ServiceBusExecutor.Smoke;

/// <summary>
/// Manifest provider used only by the operational runtime smoke path.
/// It proves the GenericMigrationJobRunner composition can resolve and execute
/// a complete provider set without requiring real external systems.
/// </summary>
public sealed class RuntimeSmokeManifestProvider : IManifestProvider
{
    public string Type => RuntimeSmokeProviderNames.Type;

    public Task<IReadOnlyList<ManifestRow>> ReadAsync(
        MigrationJobDefinition job,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<ManifestRow>>(Array.Empty<ManifestRow>());
    }
}
