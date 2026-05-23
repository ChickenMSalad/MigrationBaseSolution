using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class NoOpAzureConnectorExecutor : IAzureConnectorExecutor
{
    public Task<AzureConnectorExecutionResult> ExecuteAsync(
        AzureConnectorExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var status = request.Mode == AzureConnectorExecutionMode.ValidateOnly
            ? AzureConnectorExecutionStatus.Skipped
            : AzureConnectorExecutionStatus.Succeeded;

        var message = request.Mode switch
        {
            AzureConnectorExecutionMode.ValidateOnly =>
                "ValidateOnly mode skipped connector execution.",
            AzureConnectorExecutionMode.DryRun =>
                "DryRun mode simulated connector execution.",
            _ =>
                "No-op connector execution completed."
        };

        var evidence = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["mode"] = request.Mode.ToString(),
            ["direction"] = request.Direction.ToString()
        };

        if (!string.IsNullOrWhiteSpace(request.SourceSystem))
        {
            evidence["sourceSystem"] = request.SourceSystem;
        }

        if (!string.IsNullOrWhiteSpace(request.TargetSystem))
        {
            evidence["targetSystem"] = request.TargetSystem;
        }

        return Task.FromResult(
            new AzureConnectorExecutionResult
            {
                ItemId = request.ItemId,
                Status = status,
                SourceIdentifier = request.SourceIdentifier,
                TargetIdentifier = request.TargetIdentifier,
                Message = message,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                Evidence = evidence
            });
    }
}
