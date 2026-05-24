using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public interface IAzureEndToEndDryRunHarness
{
    Task<AzureEndToEndDryRunResult> RunAsync(
        AzureEndToEndDryRunRequest request,
        CancellationToken cancellationToken);
}
