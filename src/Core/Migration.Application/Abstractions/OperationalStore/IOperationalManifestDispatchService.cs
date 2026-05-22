using Migration.Application.Models.OperationalStore;
using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalManifestDispatchService
{
    Task<OperationalManifestDispatchResult> DispatchAsync(
        Guid runId,
        long sequenceNumber,
        string sourceId,
        string? sourcePath,
        string? sourceName,
        string? contentType,
        long? contentLength,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalManifestDispatchResult>> DispatchBatchAsync(
        Guid runId,
        IReadOnlyCollection<MigrationManifestRecord> manifestRecords,
        CancellationToken cancellationToken = default);
}
