using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public interface IAzureManifestExecutionCompletionSink
{
    Task<AzureManifestExecutionCompletionResult> CompleteAsync(
        AzureManifestExecutionCompletionRequest request,
        CancellationToken cancellationToken);
}
