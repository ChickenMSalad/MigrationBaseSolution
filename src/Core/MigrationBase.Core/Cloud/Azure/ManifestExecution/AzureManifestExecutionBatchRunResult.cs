using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionBatchRunResult
{
    public required string BatchId { get; init; }

    public IReadOnlyList<AzureManifestExecutionItemResult> ItemResults { get; init; } =
        new List<AzureManifestExecutionItemResult>();

    public int SucceededCount =>
        ItemResults.Count(result => result.Status == AzureManifestExecutionItemResultStatus.Succeeded);

    public int FailedCount =>
        ItemResults.Count(result => result.Status == AzureManifestExecutionItemResultStatus.Failed);

    public int SkippedCount =>
        ItemResults.Count(result => result.Status == AzureManifestExecutionItemResultStatus.Skipped);

    public bool HasFailures => FailedCount > 0;
}
