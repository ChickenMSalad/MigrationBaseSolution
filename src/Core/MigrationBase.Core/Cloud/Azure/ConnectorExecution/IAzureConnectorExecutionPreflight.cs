using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public interface IAzureConnectorExecutionPreflight
{
    Task<AzureConnectorExecutionPreflightResult> EvaluateAsync(
        AzureConnectorExecutionPreflightRequest request,
        CancellationToken cancellationToken);
}
