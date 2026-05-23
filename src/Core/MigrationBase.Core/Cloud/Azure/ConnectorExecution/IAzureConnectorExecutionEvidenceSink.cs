using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public interface IAzureConnectorExecutionEvidenceSink
{
    Task<AzureConnectorExecutionEvidenceResult> RecordAsync(
        AzureConnectorExecutionEvidenceRequest request,
        CancellationToken cancellationToken);
}
