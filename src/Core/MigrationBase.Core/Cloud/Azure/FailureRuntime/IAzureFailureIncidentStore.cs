using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public interface IAzureFailureIncidentStore
{
    Task<AzureFailureIncidentRecordResult> RecordAsync(
        AzureFailureIncidentRecordRequest request,
        CancellationToken cancellationToken);

    Task<AzureFailureIncidentRecord?> GetAsync(
        string incidentId,
        CancellationToken cancellationToken);
}
