using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class InMemoryAzureManifestExecutionBatchProvider : IAzureManifestExecutionBatchProvider
{
    private readonly IReadOnlyList<AzureManifestExecutionItem> items;

    public InMemoryAzureManifestExecutionBatchProvider(
        IReadOnlyList<AzureManifestExecutionItem>? items = null)
    {
        this.items = items ?? Array.Empty<AzureManifestExecutionItem>();
    }

    public Task<AzureManifestExecutionBatchResult> GetNextBatchAsync(
        AzureManifestExecutionBatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Context);

        cancellationToken.ThrowIfCancellationRequested();

        var maxItems = Math.Max(1, request.MaxItems);
        var startIndex = ParseCursor(request.Cursor);

        var batchItems = items
            .Skip(startIndex)
            .Take(maxItems)
            .ToList();

        var nextIndex = startIndex + batchItems.Count;
        var endOfManifest = nextIndex >= items.Count;
        var nextCursor = endOfManifest ? null : nextIndex.ToString();

        var batch = new AzureManifestExecutionBatch
        {
            BatchId = Guid.NewGuid().ToString("n"),
            ExecutionId = request.Context.ExecutionId,
            Cursor = request.Cursor,
            Items = batchItems
        };

        return Task.FromResult(
            new AzureManifestExecutionBatchResult
            {
                Batch = batch,
                NextCursor = nextCursor,
                IsEndOfManifest = endOfManifest
            });
    }

    private static int ParseCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return 0;
        }

        return int.TryParse(cursor, out var parsed) && parsed >= 0
            ? parsed
            : 0;
    }
}
