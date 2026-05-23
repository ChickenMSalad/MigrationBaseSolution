using System;
using System.Threading;
using System.Threading.Tasks;
using MigrationBase.Core.Cloud.Azure.ManifestExecution;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorManifestItemHandler : IAzureManifestExecutionItemHandler
{
    private readonly IAzureConnectorExecutor connectorExecutor;
    private readonly AzureManifestConnectorExecutionMapper requestMapper;
    private readonly AzureConnectorExecutionResultMapper resultMapper;
    private readonly AzureConnectorExecutionDirection direction;

    public AzureConnectorManifestItemHandler(
        IAzureConnectorExecutor connectorExecutor,
        AzureManifestConnectorExecutionMapper? requestMapper = null,
        AzureConnectorExecutionResultMapper? resultMapper = null,
        AzureConnectorExecutionDirection direction = AzureConnectorExecutionDirection.SourceRead)
    {
        this.connectorExecutor = connectorExecutor ?? throw new ArgumentNullException(nameof(connectorExecutor));
        this.requestMapper = requestMapper ?? new AzureManifestConnectorExecutionMapper();
        this.resultMapper = resultMapper ?? new AzureConnectorExecutionResultMapper();
        this.direction = direction;
    }

    public async Task<AzureManifestExecutionItemResult> ExecuteAsync(
        AzureManifestExecutionItemRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var connectorRequest = requestMapper.Map(request, direction);

        var connectorResult = await connectorExecutor.ExecuteAsync(
            connectorRequest,
            cancellationToken).ConfigureAwait(false);

        return resultMapper.Map(connectorResult);
    }
}
