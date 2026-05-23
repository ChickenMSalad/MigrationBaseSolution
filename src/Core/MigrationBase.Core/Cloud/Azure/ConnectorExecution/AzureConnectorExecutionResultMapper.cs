using System;
using MigrationBase.Core.Cloud.Azure.ManifestExecution;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionResultMapper
{
    public AzureManifestExecutionItemResult Map(
        AzureConnectorExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new AzureManifestExecutionItemResult
        {
            ItemId = result.ItemId,
            Status = MapStatus(result.Status),
            Message = result.Message,
            ErrorCode = result.ErrorCode,
            CompletedAtUtc = result.CompletedAtUtc
        };
    }

    private static AzureManifestExecutionItemResultStatus MapStatus(
        AzureConnectorExecutionStatus status)
    {
        return status switch
        {
            AzureConnectorExecutionStatus.Skipped =>
                AzureManifestExecutionItemResultStatus.Skipped,
            AzureConnectorExecutionStatus.Failed =>
                AzureManifestExecutionItemResultStatus.Failed,
            AzureConnectorExecutionStatus.Deferred =>
                AzureManifestExecutionItemResultStatus.Deferred,
            _ =>
                AzureManifestExecutionItemResultStatus.Succeeded
        };
    }
}
