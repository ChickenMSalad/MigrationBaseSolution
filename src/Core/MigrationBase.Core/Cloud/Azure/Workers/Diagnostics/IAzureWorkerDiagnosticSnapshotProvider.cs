using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.Workers.Diagnostics;

public interface IAzureWorkerDiagnosticSnapshotProvider
{
    Task<AzureWorkerDiagnosticSnapshot> CaptureAsync(CancellationToken cancellationToken = default);
}
