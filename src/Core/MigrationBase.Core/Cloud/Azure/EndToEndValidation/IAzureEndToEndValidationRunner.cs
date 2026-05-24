using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public interface IAzureEndToEndValidationRunner
{
    Task<AzureEndToEndValidationResult> RunAsync(
        AzureEndToEndValidationRequest request,
        CancellationToken cancellationToken);
}
