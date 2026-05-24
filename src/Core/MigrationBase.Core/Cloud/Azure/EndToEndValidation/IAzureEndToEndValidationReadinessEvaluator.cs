using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public interface IAzureEndToEndValidationReadinessEvaluator
{
    Task<AzureEndToEndValidationReadinessReport> EvaluateAsync(
        AzureEndToEndValidationReadinessRequest request,
        CancellationToken cancellationToken);
}
