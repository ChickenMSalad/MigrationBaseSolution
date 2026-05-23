using System;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class NoOpAzureManifestExecutionItemHandler : IAzureManifestExecutionItemHandler
{
    public Task<AzureManifestExecutionItemResult> ExecuteAsync(
        AzureManifestExecutionItemRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Item);

        cancellationToken.ThrowIfCancellationRequested();

        var status = request.IsValidateOnly
            ? AzureManifestExecutionItemResultStatus.Skipped
            : AzureManifestExecutionItemResultStatus.Succeeded;

        var message = request.IsValidateOnly
            ? "ValidateOnly mode skipped item execution."
            : request.IsDryRun
                ? "DryRun mode completed item execution simulation."
                : "No-op item execution completed.";

        return Task.FromResult(
            new AzureManifestExecutionItemResult
            {
                ItemId = request.Item.ItemId,
                Status = status,
                Message = message,
                CompletedAtUtc = DateTimeOffset.UtcNow
            });
    }
}
