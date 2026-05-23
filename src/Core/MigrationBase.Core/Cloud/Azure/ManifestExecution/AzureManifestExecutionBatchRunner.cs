using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionBatchRunner : IAzureManifestExecutionBatchRunner
{
    private readonly IAzureManifestExecutionItemHandler itemHandler;

    public AzureManifestExecutionBatchRunner(
        IAzureManifestExecutionItemHandler itemHandler)
    {
        this.itemHandler = itemHandler ?? throw new ArgumentNullException(nameof(itemHandler));
    }

    public async Task<AzureManifestExecutionBatchRunResult> RunBatchAsync(
        AzureManifestExecutionBatchRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Context);
        ArgumentNullException.ThrowIfNull(request.Batch);

        var results = new List<AzureManifestExecutionItemResult>();

        foreach (var item in request.Batch.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await itemHandler.ExecuteAsync(
                new AzureManifestExecutionItemRequest
                {
                    Context = request.Context,
                    Item = item,
                    AttemptNumber = request.AttemptNumber
                },
                cancellationToken).ConfigureAwait(false);

            results.Add(result);

            if (result.Status == AzureManifestExecutionItemResultStatus.Failed &&
                !request.ContinueOnItemFailure)
            {
                break;
            }
        }

        return new AzureManifestExecutionBatchRunResult
        {
            BatchId = request.Batch.BatchId,
            ItemResults = results
        };
    }
}
