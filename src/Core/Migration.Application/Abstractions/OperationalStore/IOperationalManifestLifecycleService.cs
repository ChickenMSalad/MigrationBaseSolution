using Migration.Application.Models.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalManifestLifecycleService
{
    Task<MigrationManifestRecord> CreateManifestRecordAsync(
        Guid runId,
        long sequenceNumber,
        string sourceId,
        string? sourcePath,
        string? sourceName,
        string? contentType,
        long? contentLength,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MigrationManifestRecord>> CreateManifestBatchAsync(
        Guid runId,
        IReadOnlyCollection<MigrationManifestRecord> records,
        CancellationToken cancellationToken = default);

    Task MarkManifestProcessingAsync(
        long manifestRecordId,
        CancellationToken cancellationToken = default);

    Task MarkManifestCompletedAsync(
        long manifestRecordId,
        CancellationToken cancellationToken = default);

    Task MarkManifestFailedAsync(
        Guid runId,
        long manifestRecordId,
        string failureReason,
        bool isRetriable,
        CancellationToken cancellationToken = default);
}
