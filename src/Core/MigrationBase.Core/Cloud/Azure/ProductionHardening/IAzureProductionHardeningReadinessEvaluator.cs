using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public interface IAzureProductionHardeningReadinessEvaluator
{
    Task<AzureProductionHardeningReadinessReport> EvaluateAsync(
        AzureProductionHardeningReadinessRequest request,
        CancellationToken cancellationToken);
}
