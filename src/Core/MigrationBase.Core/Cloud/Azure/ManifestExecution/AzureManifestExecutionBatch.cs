using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionBatch
{
    public required string BatchId { get; init; }

    public required string ExecutionId { get; init; }

    public string? Cursor { get; init; }

    public IReadOnlyList<AzureManifestExecutionItem> Items { get; init; } =
        new List<AzureManifestExecutionItem>();

    public bool HasItems => Items.Count > 0;
}
