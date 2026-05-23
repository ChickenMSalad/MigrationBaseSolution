using System;
using MigrationBase.Core.Cloud.Azure.FailureRuntime;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionFailureSignalMapper
{
    public AzureFailureSignal? Map(
        AzureConnectorExecutionRequest request,
        AzureConnectorExecutionResult result,
        int attemptNumber)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(result);

        if (result.Status != AzureConnectorExecutionStatus.Failed)
        {
            return null;
        }

        return new AzureFailureSignal
        {
            SignalId = Guid.NewGuid().ToString("n"),
            Source = "connector-execution",
            RunId = request.RunId,
            ManifestId = request.ManifestId,
            WorkItemId = request.ItemId,
            ErrorCode = result.ErrorCode,
            Message = result.Message,
            AttemptNumber = attemptNumber,
            ObservedAtUtc = result.CompletedAtUtc,
            Attributes = result.Evidence
        };
    }
}
