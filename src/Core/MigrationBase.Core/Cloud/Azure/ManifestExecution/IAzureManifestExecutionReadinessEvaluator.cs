using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public interface IAzureManifestExecutionReadinessEvaluator
{
    Task<AzureManifestExecutionReadinessReport> EvaluateAsync(
        AzureManifestExecutionReadinessRequest request,
        CancellationToken cancellationToken);
}
