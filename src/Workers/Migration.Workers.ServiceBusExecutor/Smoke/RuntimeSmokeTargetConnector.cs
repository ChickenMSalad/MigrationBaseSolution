using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Workers.ServiceBusExecutor.Smoke;

/// <summary>
/// Target connector used by the runtime smoke provider set. It performs no
/// external writes and returns success for every smoke item.
/// </summary>
public sealed class RuntimeSmokeTargetConnector : IAssetTargetConnector
{
    public string Type => RuntimeSmokeProviderNames.Type;

    public Task<MigrationResult> UpsertAsync(
        MigrationJobDefinition job,
        AssetWorkItem item,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new MigrationResult
        {
            WorkItemId = item.WorkItemId,
            Success = true,
            TargetAssetId = $"runtime-smoke:{item.WorkItemId}",
            Message = "Runtime smoke item completed without external side effects."
        });
    }
}
