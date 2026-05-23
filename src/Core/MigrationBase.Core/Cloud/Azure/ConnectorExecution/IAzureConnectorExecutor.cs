using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public interface IAzureConnectorExecutor
{
    Task<AzureConnectorExecutionResult> ExecuteAsync(
        AzureConnectorExecutionRequest request,
        CancellationToken cancellationToken);
}
