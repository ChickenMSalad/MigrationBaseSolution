using System;
using MigrationBase.Core.Cloud.Azure.ManifestExecution;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureManifestConnectorExecutionMapper
{
    public AzureConnectorExecutionRequest Map(
        AzureManifestExecutionItemRequest request,
        AzureConnectorExecutionDirection direction)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Context);
        ArgumentNullException.ThrowIfNull(request.Context.Plan);
        ArgumentNullException.ThrowIfNull(request.Item);

        var scope = request.Context.Plan.Scope;

        return new AzureConnectorExecutionRequest
        {
            ExecutionId = request.Context.ExecutionId,
            RunId = scope.RunId,
            ManifestId = scope.ManifestId,
            ItemId = request.Item.ItemId,
            SourceSystem = scope.SourceSystem,
            TargetSystem = scope.TargetSystem,
            SourceIdentifier = request.Item.SourceIdentifier,
            TargetIdentifier = request.Item.TargetIdentifier,
            Mode = MapMode(scope.Mode),
            Direction = direction,
            RequestedAtUtc = DateTimeOffset.UtcNow,
            Properties = request.Item.Properties
        };
    }

    private static AzureConnectorExecutionMode MapMode(
        AzureManifestExecutionMode mode)
    {
        return mode switch
        {
            AzureManifestExecutionMode.DryRun => AzureConnectorExecutionMode.DryRun,
            AzureManifestExecutionMode.Execute => AzureConnectorExecutionMode.Execute,
            _ => AzureConnectorExecutionMode.ValidateOnly
        };
    }
}
