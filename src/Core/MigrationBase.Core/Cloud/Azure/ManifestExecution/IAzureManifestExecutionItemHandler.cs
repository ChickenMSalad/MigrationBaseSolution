using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public interface IAzureManifestExecutionItemHandler
{
    Task<AzureManifestExecutionItemResult> ExecuteAsync(
        AzureManifestExecutionItemRequest request,
        CancellationToken cancellationToken);
}
