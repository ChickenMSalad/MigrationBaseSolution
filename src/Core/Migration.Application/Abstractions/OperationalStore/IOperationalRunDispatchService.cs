using Migration.Application.Models.OperationalStore;
using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalRunDispatchService
{
    Task<OperationalRunDispatchResult> DispatchAsync(
        string sourceSystem,
        string targetSystem,
        IReadOnlyCollection<MigrationManifestRecord> manifestRecords,
        CancellationToken cancellationToken = default);
}
