using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public interface IAzureConnectorExecutionReadinessEvaluator
{
    Task<AzureConnectorExecutionReadinessReport> EvaluateAsync(
        AzureConnectorExecutionReadinessRequest request,
        CancellationToken cancellationToken);
}
