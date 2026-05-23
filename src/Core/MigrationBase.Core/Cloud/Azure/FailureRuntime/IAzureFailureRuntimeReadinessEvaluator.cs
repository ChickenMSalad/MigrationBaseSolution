using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public interface IAzureFailureRuntimeReadinessEvaluator
{
    Task<AzureFailureRuntimeReadinessReport> EvaluateAsync(
        AzureFailureRuntimeReadinessRequest request,
        CancellationToken cancellationToken);
}
