using Migration.Application.Abstractions;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Workers.ServiceBusExecutor.Smoke;

/// <summary>
/// Source connector used only if a smoke manifest intentionally returns rows.
/// The default smoke manifest returns zero rows, but registering this connector
/// lets the generic runner resolve a complete smoke job contract.
/// </summary>
public sealed class RuntimeSmokeSourceConnector : IAssetSourceConnector
{
    public string Type => RuntimeSmokeProviderNames.Type;

    public Task<AssetEnvelope> GetAssetAsync(
        MigrationJobDefinition job,
        ManifestRow row,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sourceAssetId = string.IsNullOrWhiteSpace(row.SourceAssetId)
            ? row.RowId
            : row.SourceAssetId;

        return Task.FromResult(new AssetEnvelope
        {
            SourceAssetId = sourceAssetId,
            ExternalId = sourceAssetId,
            Path = row.SourcePath,
            SourceType = ConnectorType.LocalStorage,
            Metadata = new Dictionary<string, string?>(row.Columns, StringComparer.OrdinalIgnoreCase)
        });
    }
}
