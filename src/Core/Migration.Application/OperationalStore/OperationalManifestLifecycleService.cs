using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Application.Models.OperationalStore.Statuses;

namespace Migration.Application.OperationalStore;

public sealed class OperationalManifestLifecycleService : IOperationalManifestLifecycleService
{
    private readonly IOperationalStore _operationalStore;

    public OperationalManifestLifecycleService(
        IOperationalStore operationalStore)
    {
        _operationalStore = operationalStore;
    }

    public async Task<MigrationManifestRecord> CreateManifestRecordAsync(
        Guid runId,
        long sequenceNumber,
        string sourceId,
        string? sourcePath,
        string? sourceName,
        string? contentType,
        long? contentLength,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var record = new MigrationManifestRecord
        {
            // ManifestRecordId intentionally not assigned; SQL identity owns it.
            RunId = runId,
            SequenceNumber = sequenceNumber,
            SourceId = sourceId,
            SourcePath = sourcePath,
            SourceName = sourceName,
            ContentType = contentType,
            ContentLength = contentLength,
            Status = MigrationManifestStatuses.Created,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _operationalStore.ManifestRecords.AddAsync(
            record,
            cancellationToken);

        return record;
    }

    public async Task<IReadOnlyList<MigrationManifestRecord>> CreateManifestBatchAsync(
        Guid runId,
        IReadOnlyCollection<MigrationManifestRecord> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0)
        {
            return Array.Empty<MigrationManifestRecord>();
        }

        var now = DateTimeOffset.UtcNow;

        var preparedRecords = records
            .Select(record => new MigrationManifestRecord
            {
                RunId = runId,
                SequenceNumber = record.SequenceNumber,
                SourceId = record.SourceId,
                SourcePath = record.SourcePath,
                SourceName = record.SourceName,
                ContentType = record.ContentType,
                ContentLength = record.ContentLength,
                Status = string.IsNullOrWhiteSpace(record.Status)
                    ? MigrationManifestStatuses.Created
                    : record.Status,
                CreatedAt = record.CreatedAt == default
                    ? now
                    : record.CreatedAt,
                UpdatedAt = record.UpdatedAt ?? now
            })
            .ToArray();

        await _operationalStore.ManifestRecords.AddBatchAsync(
            preparedRecords,
            cancellationToken);

        return preparedRecords;
    }

    public Task MarkManifestProcessingAsync(
        long manifestRecordId,
        CancellationToken cancellationToken = default)
    {
        return _operationalStore.ManifestRecords.UpdateStatusAsync(
            manifestRecordId,
            MigrationManifestStatuses.Processing,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public Task MarkManifestCompletedAsync(
        long manifestRecordId,
        CancellationToken cancellationToken = default)
    {
        return _operationalStore.ManifestRecords.UpdateStatusAsync(
            manifestRecordId,
            MigrationManifestStatuses.Completed,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public async Task MarkManifestFailedAsync(
        Guid runId,
        long manifestRecordId,
        string failureReason,
        bool isRetriable,
        CancellationToken cancellationToken = default)
    {
        var failedAt = DateTimeOffset.UtcNow;

        await _operationalStore.ManifestRecords.UpdateStatusAsync(
            manifestRecordId,
            MigrationManifestStatuses.Failed,
            failedAt,
            cancellationToken);

        await _operationalStore.Failures.AddAsync(
            new MigrationFailureRecord
            {
                FailureId = Guid.NewGuid(),
                RunId = runId,
                ManifestRecordId = manifestRecordId,
                FailureType = MigrationFailureTypes.ManifestFailure,
                Message = failureReason,
                IsRetriable = isRetriable,
                CreatedAt = failedAt
            },
            cancellationToken);
    }
}
